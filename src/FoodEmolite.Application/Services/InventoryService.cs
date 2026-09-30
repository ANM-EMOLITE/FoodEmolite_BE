using FoodEmolite.Application.DTOs.Inventory;
using FoodEmolite.Application.DTOs.Realtime;
using FoodEmolite.Application.ExternalService.Interfaces;
using FoodEmolite.Application.Helpers;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Domain.Entities;
using FoodEmolite.Domain.Enums;
using FoodEmolite.Domain.Interfaces;
using FoodEmolite.Shared.Common;
using FoodEmolite.Shared.Entities;
using FoodEmolite.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace FoodEmolite.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IRealtimeNotificationService _realtimeNotificationService;
    private readonly IActivityLogService _activityLogService;

    public InventoryService(
        IUnitOfWork unitOfWork,
        ICloudinaryService cloudinaryService,
        IRealtimeNotificationService realtimeNotificationService,
        IActivityLogService activityLogService)
    {
        _unitOfWork = unitOfWork;
        _cloudinaryService = cloudinaryService;
        _realtimeNotificationService = realtimeNotificationService;
        _activityLogService = activityLogService;
    }

    // Chỉ thêm vào context, hàm gọi tự SaveChanges để đi chung transaction với thay đổi số lượng.
    public async Task TrackAsync(StoreFood food, InventoryTransactionType type, int quantityChange, long? actorId, long? orderId = null, string? referenceCode = null, string? note = null)
    {
        if (quantityChange == 0 && type != InventoryTransactionType.Initial)
            return;

        await _unitOfWork.GetRepository<InventoryTransaction>().AddAsync(new InventoryTransaction
        {
            StoreRefCode = food.StoreRefCode,
            StoreFoodId = food.Id,
            Type = type,
            QuantityChange = quantityChange,
            QuantityAfter = food.Quantity,
            UnitCost = food.CostPrice,
            OrderId = orderId,
            ReferenceCode = referenceCode,
            Note = note,
            CreatedAt = DateTimeHelper.VnNow,
            CreatedBy = actorId
        });
    }

    public async Task<BaseTableResponse<InventoryTransactionResponseDto>> SearchTransactionsAsync(long currentUserId, BaseSearchRequest<InventoryTransactionSearchRequest> request)
    {
        NormalizePaging(request);

        var store = await _unitOfWork.GetOwnedStoreAsync(currentUserId);

        if (store is null)
            return EmptyTable<InventoryTransactionResponseDto>(request.Page, request.PageSize);

        var search = request.SearchParams;

        var query =
            from t in _unitOfWork.GetRepository<InventoryTransaction>().Query().AsNoTracking()
            join f in _unitOfWork.GetRepository<StoreFood>().Query().AsNoTracking() on t.StoreFoodId equals f.Id
            where t.StoreRefCode == store.RefCode
            select new { t, f };

        if (search?.StoreFoodId is > 0)
            query = query.Where(x => x.t.StoreFoodId == search.StoreFoodId);

        if (search?.Type.HasValue == true)
            query = query.Where(x => x.t.Type == search.Type.Value);

        if (!string.IsNullOrWhiteSpace(search?.Keyword))
        {
            var keyword = search.Keyword.Trim().ToLower();
            query = query.Where(x =>
                x.f.FoodName.ToLower().Contains(keyword) ||
                x.f.ProductCode.ToLower().Contains(keyword) ||
                (x.t.ReferenceCode != null && x.t.ReferenceCode.ToLower().Contains(keyword)));
        }

        if (search?.FromDate.HasValue == true)
            query = query.Where(x => x.t.CreatedAt >= search.FromDate.Value.Date);

        if (search?.ToDate.HasValue == true)
        {
            var toDate = search.ToDate.Value.Date.AddDays(1);
            query = query.Where(x => x.t.CreatedAt < toDate);
        }

        var totalRecords = await query.CountAsync();

        query = request.Asc
            ? query.OrderBy(x => x.t.CreatedAt).ThenBy(x => x.t.Id)
            : query.OrderByDescending(x => x.t.CreatedAt).ThenByDescending(x => x.t.Id);

        var rows = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var actorNames = await GetActorNamesAsync(rows.Select(x => x.t.CreatedBy));

        return new BaseTableResponse<InventoryTransactionResponseDto>
        {
            Items = rows.Select(x => new InventoryTransactionResponseDto
            {
                Id = x.t.Id,
                CreatedAt = x.t.CreatedAt,
                StoreFoodId = x.f.Id,
                FoodName = x.f.FoodName,
                ProductCode = x.f.ProductCode,
                ThumbnailUrl = BuildThumbnail(x.f.ThumbnailUrl),
                Type = x.t.Type,
                QuantityChange = x.t.QuantityChange,
                QuantityAfter = x.t.QuantityAfter,
                UnitCost = x.t.UnitCost,
                OrderId = x.t.OrderId,
                ReferenceCode = x.t.ReferenceCode,
                Note = x.t.Note,
                ActorName = ActorName(actorNames, x.t.CreatedBy)
            }).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<BaseTableResponse<InventoryReceiptResponseDto>> SearchReceiptsAsync(long currentUserId, BaseSearchRequest<InventoryDocumentSearchRequest> request)
    {
        NormalizePaging(request);

        var store = await _unitOfWork.GetOwnedStoreAsync(currentUserId);

        if (store is null)
            return EmptyTable<InventoryReceiptResponseDto>(request.Page, request.PageSize);

        var search = request.SearchParams;

        var query = _unitOfWork.GetRepository<InventoryReceipt>()
            .Query()
            .AsNoTracking()
            .Where(x => x.StoreRefCode == store.RefCode);

        if (!string.IsNullOrWhiteSpace(search?.Keyword))
        {
            var keyword = search.Keyword.Trim().ToLower();
            query = query.Where(x =>
                x.ReceiptCode.ToLower().Contains(keyword) ||
                (x.SupplierName != null && x.SupplierName.ToLower().Contains(keyword)) ||
                (x.Note != null && x.Note.ToLower().Contains(keyword)));
        }

        query = ApplyDateRange(query, search);

        var totalRecords = await query.CountAsync();

        var receipts = await (request.Asc ? query.OrderBy(x => x.CreatedAt) : query.OrderByDescending(x => x.CreatedAt))
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var receiptIds = receipts.Select(x => x.Id).ToList();

        var itemCounts = await _unitOfWork.GetRepository<InventoryReceiptItem>()
            .Query()
            .AsNoTracking()
            .Where(x => receiptIds.Contains(x.ReceiptId))
            .GroupBy(x => x.ReceiptId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var actorNames = await GetActorNamesAsync(receipts.Select(x => x.CreatedBy));

        return new BaseTableResponse<InventoryReceiptResponseDto>
        {
            Items = receipts.Select(x => new InventoryReceiptResponseDto
            {
                Id = x.Id,
                ReceiptCode = x.ReceiptCode,
                SupplierName = x.SupplierName,
                Note = x.Note,
                TotalQuantity = x.TotalQuantity,
                TotalAmount = x.TotalAmount,
                TotalItems = itemCounts.GetValueOrDefault(x.Id),
                CreatedAt = x.CreatedAt,
                ActorName = ActorName(actorNames, x.CreatedBy)
            }).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<BaseResponse<InventoryReceiptResponseDto>> GetReceiptDetailAsync(long currentUserId, long id)
    {
        var store = await _unitOfWork.GetOwnedStoreAsync(currentUserId);

        if (store is null)
            return BaseResponse<InventoryReceiptResponseDto>.Fail("Store not found");

        var receipt = await _unitOfWork.GetRepository<InventoryReceipt>()
            .FirstOrDefaultAsync(x => x.Id == id && x.StoreRefCode == store.RefCode);

        if (receipt is null)
            return BaseResponse<InventoryReceiptResponseDto>.Fail("Receipt not found");

        var items = await (
            from i in _unitOfWork.GetRepository<InventoryReceiptItem>().Query().AsNoTracking()
            join f in _unitOfWork.GetRepository<StoreFood>().Query().AsNoTracking() on i.StoreFoodId equals f.Id
            where i.ReceiptId == receipt.Id
            orderby i.Id
            select new { i, f }
        ).ToListAsync();

        var actorNames = await GetActorNamesAsync(new[] { receipt.CreatedBy });

        return BaseResponse<InventoryReceiptResponseDto>.Success(new InventoryReceiptResponseDto
        {
            Id = receipt.Id,
            ReceiptCode = receipt.ReceiptCode,
            SupplierName = receipt.SupplierName,
            Note = receipt.Note,
            TotalQuantity = receipt.TotalQuantity,
            TotalAmount = receipt.TotalAmount,
            TotalItems = items.Count,
            CreatedAt = receipt.CreatedAt,
            ActorName = ActorName(actorNames, receipt.CreatedBy),
            Items = items.Select(x => new InventoryReceiptItemResponseDto
            {
                StoreFoodId = x.f.Id,
                FoodName = x.f.FoodName,
                ProductCode = x.f.ProductCode,
                ThumbnailUrl = BuildThumbnail(x.f.ThumbnailUrl),
                Quantity = x.i.Quantity,
                UnitCost = x.i.UnitCost,
                TotalCost = x.i.TotalCost
            }).ToList()
        });
    }

    public async Task<BaseResponse<string>> CreateReceiptAsync(long currentUserId, CreateInventoryReceiptRequestDto request)
    {
        var store = await _unitOfWork.GetOwnedStoreAsync(currentUserId);

        if (store is null)
            return BaseResponse<string>.Fail("Store not found");

        if (request.Items is null || request.Items.Count == 0)
            return BaseResponse<string>.Fail("Phiếu nhập phải có ít nhất 1 món");

        if (request.Items.Any(x => x.Quantity <= 0))
            return BaseResponse<string>.Fail("Số lượng nhập phải lớn hơn 0");

        if (request.Items.Any(x => x.UnitCost < 0))
            return BaseResponse<string>.Fail("Giá nhập không được âm");

        if (request.Items.GroupBy(x => x.StoreFoodId).Any(g => g.Count() > 1))
            return BaseResponse<string>.Fail("Mỗi món chỉ được xuất hiện 1 lần trong phiếu");

        var repoFood = _unitOfWork.GetRepository<StoreFood>();
        var foodIds = request.Items.Select(x => x.StoreFoodId).ToList();

        var foods = await repoFood
            .Query()
            .Where(x => foodIds.Contains(x.Id) && x.StoreRefCode == store.RefCode && !x.IsDeleted)
            .ToListAsync();

        if (foods.Count != foodIds.Count)
            return BaseResponse<string>.Fail("Có món không thuộc cửa hàng hoặc đã bị xoá");

        var repoReceipt = _unitOfWork.GetRepository<InventoryReceipt>();
        var receiptCount = await repoReceipt.Query().CountAsync(x => x.StoreRefCode == store.RefCode);

        var receipt = new InventoryReceipt
        {
            StoreRefCode = store.RefCode!,
            ReceiptCode = $"PN{receiptCount + 1:D5}",
            SupplierName = request.SupplierName?.Trim(),
            Note = request.Note?.Trim(),
            TotalQuantity = request.Items.Sum(x => x.Quantity),
            TotalAmount = request.Items.Sum(x => x.Quantity * x.UnitCost),
            CreatedAt = DateTimeHelper.VnNow,
            CreatedBy = currentUserId
        };

        await repoReceipt.AddAsync(receipt);
        await _unitOfWork.SaveChangesAsync();

        var repoItem = _unitOfWork.GetRepository<InventoryReceiptItem>();

        foreach (var item in request.Items)
        {
            var food = foods.First(x => x.Id == item.StoreFoodId);

            await repoItem.AddAsync(new InventoryReceiptItem
            {
                ReceiptId = receipt.Id,
                StoreFoodId = food.Id,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost,
                TotalCost = item.Quantity * item.UnitCost,
                CreatedAt = DateTimeHelper.VnNow,
                CreatedBy = currentUserId
            });

            // Giá vốn bình quân gia quyền
            var currentStock = Math.Max(food.Quantity, 0);
            food.CostPrice = currentStock == 0
                ? item.UnitCost
                : Math.Round((currentStock * food.CostPrice + item.Quantity * item.UnitCost) / (currentStock + item.Quantity), 2);

            food.Quantity += item.Quantity;
            repoFood.Update(food);

            await TrackAsync(food, InventoryTransactionType.Import, item.Quantity, currentUserId, referenceCode: receipt.ReceiptCode, note: receipt.SupplierName);
        }

        await _unitOfWork.SaveChangesAsync();

        await BroadcastQuantitiesAsync(foods);

        await _activityLogService.LogAgentActionAsync(currentUserId, null, "IMPORT_STOCK",
            $"Nhập hàng phiếu \"{receipt.ReceiptCode}\": {receipt.TotalQuantity} sản phẩm, tổng tiền {receipt.TotalAmount:N0}đ", store.RefCode);

        return BaseResponse<string>.Success(receipt.ReceiptCode);
    }

    public async Task<BaseTableResponse<InventoryStocktakeResponseDto>> SearchStocktakesAsync(long currentUserId, BaseSearchRequest<InventoryDocumentSearchRequest> request)
    {
        NormalizePaging(request);

        var store = await _unitOfWork.GetOwnedStoreAsync(currentUserId);

        if (store is null)
            return EmptyTable<InventoryStocktakeResponseDto>(request.Page, request.PageSize);

        var search = request.SearchParams;

        var query = _unitOfWork.GetRepository<InventoryStocktake>()
            .Query()
            .AsNoTracking()
            .Where(x => x.StoreRefCode == store.RefCode);

        if (!string.IsNullOrWhiteSpace(search?.Keyword))
        {
            var keyword = search.Keyword.Trim().ToLower();
            query = query.Where(x =>
                x.StocktakeCode.ToLower().Contains(keyword) ||
                (x.Note != null && x.Note.ToLower().Contains(keyword)));
        }

        query = ApplyDateRange(query, search);

        var totalRecords = await query.CountAsync();

        var stocktakes = await (request.Asc ? query.OrderBy(x => x.CreatedAt) : query.OrderByDescending(x => x.CreatedAt))
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        var actorNames = await GetActorNamesAsync(stocktakes.Select(x => x.CreatedBy));

        return new BaseTableResponse<InventoryStocktakeResponseDto>
        {
            Items = stocktakes.Select(x => new InventoryStocktakeResponseDto
            {
                Id = x.Id,
                StocktakeCode = x.StocktakeCode,
                Note = x.Note,
                TotalItems = x.TotalItems,
                TotalDifference = x.TotalDifference,
                DifferenceValue = x.DifferenceValue,
                CreatedAt = x.CreatedAt,
                ActorName = ActorName(actorNames, x.CreatedBy)
            }).ToList(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalRecords = totalRecords
        };
    }

    public async Task<BaseResponse<InventoryStocktakeResponseDto>> GetStocktakeDetailAsync(long currentUserId, long id)
    {
        var store = await _unitOfWork.GetOwnedStoreAsync(currentUserId);

        if (store is null)
            return BaseResponse<InventoryStocktakeResponseDto>.Fail("Store not found");

        var stocktake = await _unitOfWork.GetRepository<InventoryStocktake>()
            .FirstOrDefaultAsync(x => x.Id == id && x.StoreRefCode == store.RefCode);

        if (stocktake is null)
            return BaseResponse<InventoryStocktakeResponseDto>.Fail("Stocktake not found");

        var items = await (
            from i in _unitOfWork.GetRepository<InventoryStocktakeItem>().Query().AsNoTracking()
            join f in _unitOfWork.GetRepository<StoreFood>().Query().AsNoTracking() on i.StoreFoodId equals f.Id
            where i.StocktakeId == stocktake.Id
            orderby i.Id
            select new { i, f }
        ).ToListAsync();

        var actorNames = await GetActorNamesAsync(new[] { stocktake.CreatedBy });

        return BaseResponse<InventoryStocktakeResponseDto>.Success(new InventoryStocktakeResponseDto
        {
            Id = stocktake.Id,
            StocktakeCode = stocktake.StocktakeCode,
            Note = stocktake.Note,
            TotalItems = stocktake.TotalItems,
            TotalDifference = stocktake.TotalDifference,
            DifferenceValue = stocktake.DifferenceValue,
            CreatedAt = stocktake.CreatedAt,
            ActorName = ActorName(actorNames, stocktake.CreatedBy),
            Items = items.Select(x => new InventoryStocktakeItemResponseDto
            {
                StoreFoodId = x.f.Id,
                FoodName = x.f.FoodName,
                ProductCode = x.f.ProductCode,
                ThumbnailUrl = BuildThumbnail(x.f.ThumbnailUrl),
                SystemQuantity = x.i.SystemQuantity,
                ActualQuantity = x.i.ActualQuantity,
                Difference = x.i.Difference,
                UnitCost = x.i.UnitCost,
                Note = x.i.Note
            }).ToList()
        });
    }

    public async Task<BaseResponse<string>> CreateStocktakeAsync(long currentUserId, CreateInventoryStocktakeRequestDto request)
    {
        var store = await _unitOfWork.GetOwnedStoreAsync(currentUserId);

        if (store is null)
            return BaseResponse<string>.Fail("Store not found");

        if (request.Items is null || request.Items.Count == 0)
            return BaseResponse<string>.Fail("Phiếu kiểm kho phải có ít nhất 1 món");

        if (request.Items.Any(x => x.ActualQuantity < 0))
            return BaseResponse<string>.Fail("Số lượng thực tế không được âm");

        if (request.Items.GroupBy(x => x.StoreFoodId).Any(g => g.Count() > 1))
            return BaseResponse<string>.Fail("Mỗi món chỉ được xuất hiện 1 lần trong phiếu");

        var repoFood = _unitOfWork.GetRepository<StoreFood>();
        var foodIds = request.Items.Select(x => x.StoreFoodId).ToList();

        var foods = await repoFood
            .Query()
            .Where(x => foodIds.Contains(x.Id) && x.StoreRefCode == store.RefCode && !x.IsDeleted)
            .ToListAsync();

        if (foods.Count != foodIds.Count)
            return BaseResponse<string>.Fail("Có món không thuộc cửa hàng hoặc đã bị xoá");

        var repoStocktake = _unitOfWork.GetRepository<InventoryStocktake>();
        var stocktakeCount = await repoStocktake.Query().CountAsync(x => x.StoreRefCode == store.RefCode);

        var lines = request.Items.Select(item =>
        {
            var food = foods.First(x => x.Id == item.StoreFoodId);
            return new { Item = item, Food = food, SystemQuantity = food.Quantity, Difference = item.ActualQuantity - food.Quantity };
        }).ToList();

        var stocktake = new InventoryStocktake
        {
            StoreRefCode = store.RefCode!,
            StocktakeCode = $"KK{stocktakeCount + 1:D5}",
            Note = request.Note?.Trim(),
            TotalItems = lines.Count,
            TotalDifference = lines.Sum(x => x.Difference),
            DifferenceValue = lines.Sum(x => x.Difference * x.Food.CostPrice),
            CreatedAt = DateTimeHelper.VnNow,
            CreatedBy = currentUserId
        };

        await repoStocktake.AddAsync(stocktake);
        await _unitOfWork.SaveChangesAsync();

        var repoItem = _unitOfWork.GetRepository<InventoryStocktakeItem>();

        foreach (var line in lines)
        {
            await repoItem.AddAsync(new InventoryStocktakeItem
            {
                StocktakeId = stocktake.Id,
                StoreFoodId = line.Food.Id,
                SystemQuantity = line.SystemQuantity,
                ActualQuantity = line.Item.ActualQuantity,
                Difference = line.Difference,
                UnitCost = line.Food.CostPrice,
                Note = line.Item.Note?.Trim(),
                CreatedAt = DateTimeHelper.VnNow,
                CreatedBy = currentUserId
            });

            if (line.Difference == 0)
                continue;

            line.Food.Quantity = line.Item.ActualQuantity;
            repoFood.Update(line.Food);

            await TrackAsync(line.Food, InventoryTransactionType.Stocktake, line.Difference, currentUserId, referenceCode: stocktake.StocktakeCode, note: line.Item.Note?.Trim());
        }

        await _unitOfWork.SaveChangesAsync();

        await BroadcastQuantitiesAsync(lines.Where(x => x.Difference != 0).Select(x => x.Food));

        await _activityLogService.LogAgentActionAsync(currentUserId, null, "STOCKTAKE",
            $"Kiểm kho phiếu \"{stocktake.StocktakeCode}\": {stocktake.TotalItems} món, chênh lệch {stocktake.TotalDifference:+#;-#;0} sản phẩm", store.RefCode);

        return BaseResponse<string>.Success(stocktake.StocktakeCode);
    }

    private async Task BroadcastQuantitiesAsync(IEnumerable<StoreFood> foods)
    {
        foreach (var food in foods)
        {
            await _realtimeNotificationService.NotifyFoodQuantityChangedAsync(new FoodQuantityChangedDto
            {
                StoreFoodId = food.Id,
                StoreRefCode = food.StoreRefCode,
                Quantity = food.Quantity
            });
        }
    }

    private async Task<Dictionary<long, string>> GetActorNamesAsync(IEnumerable<long?> actorIds)
    {
        var ids = actorIds.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();

        if (ids.Count == 0)
            return [];

        var accounts = await (
            from a in _unitOfWork.GetRepository<Account>().Query().AsNoTracking()
            join p in _unitOfWork.GetRepository<AccountProfile>().Query().AsNoTracking() on a.Id equals p.AccountId into profiles
            from p in profiles.DefaultIfEmpty()
            where ids.Contains(a.Id)
            select new { a.Id, a.Username, FullName = p != null ? p.FullName : null }
        ).ToListAsync();

        return accounts
            .GroupBy(x => x.Id)
            .ToDictionary(g => g.Key, g => g.Select(x => !string.IsNullOrWhiteSpace(x.FullName) ? x.FullName! : x.Username).First());
    }

    private static string? ActorName(Dictionary<long, string> names, long? actorId)
        => actorId.HasValue && names.TryGetValue(actorId.Value, out var name) ? name : null;

    private string? BuildThumbnail(string? thumbnailUrl)
        => string.IsNullOrWhiteSpace(thumbnailUrl) ? null : _cloudinaryService.BuildImageUrl(thumbnailUrl);

    private static IQueryable<T> ApplyDateRange<T>(IQueryable<T> query, InventoryDocumentSearchRequest? search) where T : BaseEntity
    {
        if (search?.FromDate.HasValue == true)
            query = query.Where(x => x.CreatedAt >= search.FromDate.Value.Date);

        if (search?.ToDate.HasValue == true)
        {
            var toDate = search.ToDate.Value.Date.AddDays(1);
            query = query.Where(x => x.CreatedAt < toDate);
        }

        return query;
    }

    private static void NormalizePaging<T>(BaseSearchRequest<T> request)
    {
        request.Page = request.Page <= 0 ? 1 : request.Page;
        request.PageSize = request.PageSize <= 0 ? 20 : Math.Min(request.PageSize, 100);
    }

    private static BaseTableResponse<T> EmptyTable<T>(int page, int pageSize) => new()
    {
        Items = [],
        Page = page,
        PageSize = pageSize,
        TotalRecords = 0
    };
}
