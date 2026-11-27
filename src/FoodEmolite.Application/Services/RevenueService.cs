using FoodEmolite.Shared.Common;
﻿using FoodEmolite.Domain.Enums;
using FoodEmolite.Application.DTOs.Revenue;
using FoodEmolite.Application.ExternalService.Interfaces;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Domain.Entities;
using FoodEmolite.Domain.Interfaces;
using FoodEmolite.Shared.Entities;
using FoodEmolite.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace FoodEmolite.Application.Services;

public class RevenueService : IRevenueService
{
    private const int LowStockThreshold = 5;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICloudinaryService _cloudinaryService;

    public RevenueService(IUnitOfWork unitOfWork, ICloudinaryService cloudinaryService)
    {
        _unitOfWork = unitOfWork;
        _cloudinaryService = cloudinaryService;
    }

    public async Task<BaseResponse<AgentRevenueResponseDto>> GetAgentRevenueAsync(long currentUserId, RevenueQueryDto request)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();
        var repoOrder = _unitOfWork.GetRepository<Order>();

        var fromDate = request.FromDate?.Date;
        var toDate = request.ToDate?.Date.AddDays(1).AddTicks(-1);
        var groupBy = request.GroupBy?.ToLower() == "month" ? "month" : "day";

        var store = await repoStore.FirstOrDefaultAsync(x =>
            x.OwnerAccountId == currentUserId &&
            !x.IsDeleted);

        if (store is null)
        {
            return BaseResponse<AgentRevenueResponseDto>.Fail("Store not found");
        }

        var orderQuery = repoOrder
            .Query()
            .AsNoTracking()
            .Where(x =>
                x.StoreRefCode == store.RefCode);

        if (fromDate.HasValue)
        {
            orderQuery = orderQuery.Where(x => x.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            orderQuery = orderQuery.Where(x => x.CreatedAt <= toDate.Value);
        }

        var allOrders = await orderQuery
            .Select(x => new
            {
                x.OrderStatus,
                x.PaymentStatus,
                x.TotalAmount
            })
            .ToListAsync();

        // Doanh thu: chỉ đơn PAID và chưa huỷ
        var paidOrderQuery = orderQuery
            .Where(x => x.PaymentStatus == PaymentStatus.Paid && x.OrderStatus != OrderStatus.Cancelled);

        var totalOrders = await orderQuery.CountAsync();

        var totalRevenue = await paidOrderQuery
            .SumAsync(x => (decimal?)x.TotalAmount) ?? 0;

        var paidOrders = await paidOrderQuery
            .Select(x => new
            {
                x.Id,
                x.CreatedAt,
                x.TotalAmount,
                x.OrderStatus
            })
            .ToListAsync();

        var orderCosts = await GetOrderCostsAsync(paidOrderQuery);

        var lineChart = BuildLineChart(
            paidOrders.Select(x => new RevenueRawItem
            {
                CreatedAt = x.CreatedAt,
                Amount = x.TotalAmount,
                Cost = orderCosts.GetValueOrDefault(x.Id)
            }).ToList(),
            groupBy,
            fromDate,
            toDate);

        // Trạng thái gộp giống trang đơn hàng: đơn huỷ luôn là CANCELLED, còn lại theo PAID / UNPAID.
        var pieChart = allOrders
            .GroupBy(x => x.OrderStatus == OrderStatus.Cancelled
                ? "CANCELLED"
                : EnumCode.ToCode(x.PaymentStatus))
            .Select(g => new RevenuePieChartDto
            {
                Label = g.Key,
                Value = g.Sum(x => x.TotalAmount)
            })
            .ToList();

        return BaseResponse<AgentRevenueResponseDto>.Success(
            new AgentRevenueResponseDto
            {
                TotalOrders = totalOrders,
                TotalCancelledOrders = allOrders.Count(x => x.OrderStatus == OrderStatus.Cancelled),
                TotalRevenue = totalRevenue,
                TotalCost = orderCosts.Values.Sum(),
                TotalProfit = totalRevenue - orderCosts.Values.Sum(),
                LineChart = lineChart,
                PieChart = pieChart
            });
    }

    public async Task<BaseResponse<List<TopSellingProductDto>>> GetAgentTopSellingProductsAsync(long currentUserId, RevenueQueryDto request, int top = 10)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();

        var store = await repoStore.FirstOrDefaultAsync(x =>
            x.OwnerAccountId == currentUserId &&
            !x.IsDeleted);

        if (store is null)
            return BaseResponse<List<TopSellingProductDto>>.Fail("Store not found");

        var fromDate = request.FromDate?.Date;
        var toDate = request.ToDate?.Date.AddDays(1).AddTicks(-1);

        var products = await BuildProductRevenueAsync(store.RefCode, fromDate, toDate);

        var topProducts = products
            .OrderByDescending(x => x.QuantitySold)
            .Take(top <= 0 ? 10 : top)
            .ToList();

        return BaseResponse<List<TopSellingProductDto>>.Success(topProducts);
    }

    public async Task<BaseTableResponse<TopSellingProductDto>> GetAgentProductRevenueAsync(long currentUserId, BaseSearchRequest<ProductRevenueSearchRequest> request)
    {
        var repoStore = _unitOfWork.GetRepository<Store>();

        var store = await repoStore.FirstOrDefaultAsync(x =>
            x.OwnerAccountId == currentUserId &&
            !x.IsDeleted);

        if (store is null)
        {
            return new BaseTableResponse<TopSellingProductDto>
            {
                Items = [],
                Page = request.Page,
                PageSize = request.PageSize,
                TotalRecords = 0
            };
        }

        var search = request.SearchParams;
        var fromDate = search?.FromDate?.Date;
        var toDate = search?.ToDate?.Date.AddDays(1).AddTicks(-1);

        var products = await BuildProductRevenueAsync(store.RefCode, fromDate, toDate);

        IEnumerable<TopSellingProductDto> filtered = products;

        if (!string.IsNullOrWhiteSpace(search?.Keyword))
        {
            var keyword = search.Keyword.Trim().ToLower();
            filtered = filtered.Where(x => x.FoodName.ToLower().Contains(keyword));
        }

        filtered = request.SortBy?.ToLower() switch
        {
            "quantitysold" => request.Asc
                ? filtered.OrderBy(x => x.QuantitySold)
                : filtered.OrderByDescending(x => x.QuantitySold),

            "profit" => request.Asc
                ? filtered.OrderBy(x => x.Profit)
                : filtered.OrderByDescending(x => x.Profit),

            "foodname" => request.Asc
                ? filtered.OrderBy(x => x.FoodName)
                : filtered.OrderByDescending(x => x.FoodName),

            _ => request.Asc
                ? filtered.OrderBy(x => x.Revenue)
                : filtered.OrderByDescending(x => x.Revenue)
        };

        var materialized = filtered.ToList();
        var totalRecords = materialized.Count;

        var pageItems = materialized
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new BaseTableResponse<TopSellingProductDto>
        {
            Items = pageItems,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<BaseResponse<AgentDashboardResponseDto>> GetAgentDashboardAsync(long currentUserId, RevenueQueryDto request)
    {
        var queryStore = _unitOfWork.GetRepository<Store>().Query().AsNoTracking();
        var queryOrder = _unitOfWork.GetRepository<Order>().Query().AsNoTracking();
        var queryStoreFood = _unitOfWork.GetRepository<StoreFood>().Query().AsNoTracking();

        var store = await queryStore.FirstOrDefaultAsync(x => x.OwnerAccountId == currentUserId && !x.IsDeleted);

        if (store is null)
            return BaseResponse<AgentDashboardResponseDto>.Fail("Store not found");

        var today = DateTimeHelper.VnNow.Date;
        var toDate = (request.ToDate ?? today).Date;
        var fromDate = (request.FromDate ?? toDate.AddDays(-6)).Date;

        if (fromDate > toDate)
            (fromDate, toDate) = (toDate, fromDate);

        var totalDays = (toDate - fromDate).Days + 1;
        var previousToDate = fromDate.AddDays(-1);
        var previousFromDate = previousToDate.AddDays(-(totalDays - 1));
        var toEnd = toDate.AddDays(1).AddTicks(-1);

        var storeOrders = queryOrder.Where(x => x.StoreRefCode == store.RefCode && !x.IsDelete);

        var orders = await storeOrders
            .Where(x => x.CreatedAt >= previousFromDate && x.CreatedAt <= toEnd)
            .Select(x => new DashboardOrderItem
            {
                CreatedAt = x.CreatedAt,
                TotalAmount = x.TotalAmount,
                IsCancelled = x.OrderStatus == OrderStatus.Cancelled,
                IsPaid = x.PaymentStatus == PaymentStatus.Paid,
                CustomerKey = x.CustomerId != null
                    ? "C" + x.CustomerId
                    : x.CustomerAccountId != null ? "A" + x.CustomerAccountId : null
            })
            .ToListAsync();

        var pendingUnpaidOrders = await storeOrders
            .CountAsync(x => x.OrderStatus != OrderStatus.Cancelled && x.PaymentStatus != PaymentStatus.Paid);

        var foods = await queryStoreFood
            .Where(x => x.StoreRefCode == store.RefCode && !x.IsDeleted)
            .Select(x => new { x.Quantity, x.IsAvailable })
            .ToListAsync();

        var currentOrders = orders.Where(x => x.CreatedAt >= fromDate).ToList();
        var previousOrders = orders.Where(x => x.CreatedAt < fromDate).ToList();

        return BaseResponse<AgentDashboardResponseDto>.Success(new AgentDashboardResponseDto
        {
            StoreName = store.StoreName,
            PendingUnpaidOrders = pendingUnpaidOrders,
            FromDate = fromDate,
            ToDate = toDate,
            PreviousFromDate = previousFromDate,
            PreviousToDate = previousToDate,
            Current = BuildDashboardSummary(currentOrders),
            Previous = BuildDashboardSummary(previousOrders),
            Daily = BuildDashboardDaily(currentOrders, fromDate, toDate),
            PreviousDaily = BuildDashboardDaily(previousOrders, previousFromDate, previousToDate),
            OutOfStock = foods.Count(x => x.Quantity <= 0),
            LowStock = foods.Count(x => x.Quantity > 0 && x.Quantity < LowStockThreshold),
            Unavailable = foods.Count(x => !x.IsAvailable)
        });
    }

    private static DashboardSummaryDto BuildDashboardSummary(List<DashboardOrderItem> orders)
    {
        var active = orders.Where(x => !x.IsCancelled).ToList();
        var paid = active.Where(x => x.IsPaid).ToList();
        var unpaid = active.Where(x => !x.IsPaid).ToList();
        var cancelled = orders.Where(x => x.IsCancelled).ToList();

        return new DashboardSummaryDto
        {
            TotalOrders = orders.Count,
            TotalAmount = active.Sum(x => x.TotalAmount),
            Revenue = paid.Sum(x => x.TotalAmount),
            PaidOrders = paid.Count,
            UnpaidOrders = unpaid.Count,
            UnpaidAmount = unpaid.Sum(x => x.TotalAmount),
            CancelledOrders = cancelled.Count,
            CancelledAmount = cancelled.Sum(x => x.TotalAmount),
            Customers = orders.Where(x => x.CustomerKey != null).Select(x => x.CustomerKey).Distinct().Count()
        };
    }

    private static List<DashboardDailyDto> BuildDashboardDaily(List<DashboardOrderItem> orders, DateTime fromDate, DateTime toDate)
    {
        var byDay = orders.GroupBy(x => x.CreatedAt.Date).ToDictionary(g => g.Key, g => g.ToList());
        var days = new List<DashboardDailyDto>();

        for (var d = fromDate; d <= toDate; d = d.AddDays(1))
        {
            var items = byDay.GetValueOrDefault(d) ?? [];

            days.Add(new DashboardDailyDto
            {
                Date = d,
                Orders = items.Count,
                Customers = items.Where(x => x.CustomerKey != null).Select(x => x.CustomerKey).Distinct().Count(),
                CancelledAmount = items.Where(x => x.IsCancelled).Sum(x => x.TotalAmount),
                Revenue = items.Where(x => !x.IsCancelled && x.IsPaid).Sum(x => x.TotalAmount),
                TotalAmount = items.Where(x => !x.IsCancelled).Sum(x => x.TotalAmount)
            });
        }

        return days;
    }

    private class DashboardOrderItem
    {
        public DateTime CreatedAt { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsCancelled { get; set; }
        public bool IsPaid { get; set; }
        public string? CustomerKey { get; set; }
    }

    /// <summary>
    /// Gộp OrderItem theo món cho 1 cửa hàng trong khoảng ngày, chỉ tính đơn PaymentStatus = PAID và chưa bị huỷ.
    /// Dùng chung cho top-selling-products và bảng doanh thu theo sản phẩm.
    /// </summary>
    private async Task<List<TopSellingProductDto>> BuildProductRevenueAsync(string? storeRefCode, DateTime? fromDate, DateTime? toDate)
    {
        var repoOrder = _unitOfWork.GetRepository<Order>();
        var repoOrderItem = _unitOfWork.GetRepository<OrderItem>();
        var repoStoreFood = _unitOfWork.GetRepository<StoreFood>();
        var repoStore = _unitOfWork.GetRepository<Store>();

        var orderQuery = repoOrder
            .Query()
            .AsNoTracking()
            .Where(x => x.PaymentStatus == PaymentStatus.Paid && x.OrderStatus != OrderStatus.Cancelled);

        if (!string.IsNullOrWhiteSpace(storeRefCode))
        {
            orderQuery = orderQuery.Where(x => x.StoreRefCode == storeRefCode);
        }

        if (fromDate.HasValue)
        {
            orderQuery = orderQuery.Where(x => x.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            orderQuery = orderQuery.Where(x => x.CreatedAt <= toDate.Value);
        }

        var items = await (
            from oi in repoOrderItem.Query().AsNoTracking()
            join o in orderQuery on oi.OrderId equals o.Id
            join f in repoStoreFood.Query().AsNoTracking() on oi.StoreFoodId equals f.Id
            join s in repoStore.Query().AsNoTracking() on o.StoreRefCode equals s.RefCode
            select new
            {
                oi.StoreFoodId,
                f.FoodName,
                f.ThumbnailUrl,
                oi.Quantity,
                oi.TotalPrice,
                oi.CostPrice,
                StoreRefCode = o.StoreRefCode,
                s.StoreName
            }
        ).ToListAsync();

        return items
            .GroupBy(x => new { x.StoreFoodId, x.FoodName, x.ThumbnailUrl, x.StoreRefCode, x.StoreName })
            .Select(g => new TopSellingProductDto
            {
                StoreFoodId = g.Key.StoreFoodId,
                FoodName = g.Key.FoodName,
                ThumbnailUrl = !string.IsNullOrWhiteSpace(g.Key.ThumbnailUrl)
                    ? _cloudinaryService.BuildImageUrl(g.Key.ThumbnailUrl)
                    : null,
                QuantitySold = g.Sum(x => x.Quantity),
                Revenue = g.Sum(x => x.TotalPrice),
                Cost = g.Sum(x => x.Quantity * x.CostPrice),
                Profit = g.Sum(x => x.TotalPrice - x.Quantity * x.CostPrice),
                StoreRefCode = g.Key.StoreRefCode,
                StoreName = g.Key.StoreName
            })
            .ToList();
    }

    private async Task<Dictionary<long, decimal>> GetOrderCostsAsync(IQueryable<Order> orders)
    {
        var orderIds = orders.Select(x => x.Id);

        return await _unitOfWork.GetRepository<OrderItem>()
            .Query()
            .AsNoTracking()
            .Where(x => orderIds.Contains(x.OrderId))
            .GroupBy(x => x.OrderId)
            .Select(g => new { g.Key, Cost = g.Sum(x => x.Quantity * x.CostPrice) })
            .ToDictionaryAsync(x => x.Key, x => x.Cost);
    }

    private static List<RevenueLineChartDto> BuildLineChart(
        List<RevenueRawItem> items,
        string groupBy,
        DateTime? fromDate,
        DateTime? toDate)
    {
        if (items.Count == 0 && (!fromDate.HasValue || !toDate.HasValue))
        {
            return [];
        }

        var start = fromDate?.Date ?? items.Min(x => x.CreatedAt.Date);
        var end = toDate?.Date ?? items.Max(x => x.CreatedAt.Date);

        if (groupBy == "month")
        {
            var byMonth = items
                .GroupBy(x => new DateTime(x.CreatedAt.Year, x.CreatedAt.Month, 1))
                .ToDictionary(g => g.Key, g => (Revenue: g.Sum(x => x.Amount), Cost: g.Sum(x => x.Cost), Count: g.Count()));

            var months = new List<RevenueLineChartDto>();

            for (var m = new DateTime(start.Year, start.Month, 1); m <= end; m = m.AddMonths(1))
            {
                byMonth.TryGetValue(m, out var v);
                months.Add(new RevenueLineChartDto
                {
                    Label = m.ToString("MM/yyyy"),
                    Revenue = v.Revenue,
                    Cost = v.Cost,
                    Profit = v.Revenue - v.Cost,
                    OrderCount = v.Count
                });
            }

            return months;
        }

        var byDay = items
            .GroupBy(x => x.CreatedAt.Date)
            .ToDictionary(g => g.Key, g => (Revenue: g.Sum(x => x.Amount), Cost: g.Sum(x => x.Cost), Count: g.Count()));

        var days = new List<RevenueLineChartDto>();

        for (var d = start; d <= end; d = d.AddDays(1))
        {
            byDay.TryGetValue(d, out var v);
            days.Add(new RevenueLineChartDto
            {
                Label = d.ToString("dd/MM/yyyy"),
                Revenue = v.Revenue,
                Cost = v.Cost,
                Profit = v.Revenue - v.Cost,
                OrderCount = v.Count
            });
        }

        return days;
    }

    private class RevenueRawItem
    {
        public DateTime CreatedAt { get; set; }
        public decimal Amount { get; set; }
        public decimal Cost { get; set; }
    }
}