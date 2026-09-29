using FoodEmolite.Application.DTOs.Notification;
using FoodEmolite.Shared.Common;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Domain.Entities;
using FoodEmolite.Domain.Interfaces;
using FoodEmolite.Shared.Responses;
using Microsoft.EntityFrameworkCore;

namespace FoodEmolite.Application.Services;

public class StoreNotificationService : IStoreNotificationService
{
    private readonly IUnitOfWork _unitOfWork;

    public StoreNotificationService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<StoreNotification> CreateNewOrderAsync(Order order, string customerName)
    {
        var repo = _unitOfWork.GetRepository<StoreNotification>();

        var notification = new StoreNotification
        {
            StoreRefCode = order.StoreRefCode,
            Type = "NEW_ORDER",
            OrderId = order.Id,
            OrderCode = order.OrderCode,
            CustomerName = customerName,
            TotalAmount = order.TotalAmount,
            IsRead = false,
            CreatedAt = DateTimeHelper.VnNow
        };

        await repo.AddAsync(notification);
        await _unitOfWork.SaveChangesAsync();

        return notification;
    }

    public async Task<BaseResponse<StoreNotificationListResponseDto>> GetMyStoreAsync(long currentUserId, int page, int pageSize, bool? isRead = null, int? days = null)
    {
        var storeRefCode = await GetOwnedStoreRefCodeAsync(currentUserId);

        if (storeRefCode is null)
            return BaseResponse<StoreNotificationListResponseDto>.Fail("Store not found");

        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);

        var query = _unitOfWork.GetRepository<StoreNotification>()
            .Query()
            .AsNoTracking()
            .Where(x => x.StoreRefCode == storeRefCode);

        // Trang Thông báo chỉ xem N ngày gần nhất — số chưa đọc cũng tính trong khoảng này cho khớp danh sách.
        if (days is > 0)
        {
            var fromDate = DateTimeHelper.VnNow.Date.AddDays(-(days.Value - 1));
            query = query.Where(x => x.CreatedAt >= fromDate);
        }

        // Số chưa đọc không phụ thuộc bộ lọc đã đọc / chưa đọc.
        var unreadCount = await query.CountAsync(x => !x.IsRead);

        if (isRead.HasValue)
            query = query.Where(x => x.IsRead == isRead.Value);

        var totalRecords = await query.CountAsync();

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new StoreNotificationResponseDto
            {
                Id = x.Id,
                Type = x.Type,
                OrderId = x.OrderId,
                OrderCode = x.OrderCode,
                StoreRefCode = x.StoreRefCode,
                CustomerName = x.CustomerName,
                TotalAmount = x.TotalAmount,
                IsRead = x.IsRead,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();

        return BaseResponse<StoreNotificationListResponseDto>.Success(new StoreNotificationListResponseDto
        {
            Items = items,
            UnreadCount = unreadCount,
            TotalRecords = totalRecords
        });
    }

    public async Task<BaseResponse<string>> MarkReadAsync(long currentUserId, long id)
    {
        var storeRefCode = await GetOwnedStoreRefCodeAsync(currentUserId);

        if (storeRefCode is null)
            return BaseResponse<string>.Fail("Store not found");

        var repo = _unitOfWork.GetRepository<StoreNotification>();
        var notification = await repo.FirstOrDefaultAsync(x => x.Id == id && x.StoreRefCode == storeRefCode);

        if (notification is null)
            return BaseResponse<string>.Fail("Notification not found");

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTimeHelper.VnNow;
            repo.Update(notification);
            await _unitOfWork.SaveChangesAsync();
        }

        return BaseResponse<string>.Success("Đã đánh dấu đã đọc");
    }

    public async Task<BaseResponse<string>> MarkAllReadAsync(long currentUserId)
    {
        var storeRefCode = await GetOwnedStoreRefCodeAsync(currentUserId);

        if (storeRefCode is null)
            return BaseResponse<string>.Fail("Store not found");

        var repo = _unitOfWork.GetRepository<StoreNotification>();
        var now = DateTimeHelper.VnNow;

        var unread = await repo.Query()
            .Where(x => x.StoreRefCode == storeRefCode && !x.IsRead)
            .ToListAsync();

        foreach (var notification in unread)
        {
            notification.IsRead = true;
            notification.ReadAt = now;
            repo.Update(notification);
        }

        if (unread.Count > 0)
            await _unitOfWork.SaveChangesAsync();

        return BaseResponse<string>.Success("Đã đánh dấu tất cả là đã đọc");
    }

    private async Task<string?> GetOwnedStoreRefCodeAsync(long currentUserId)
    {
        var store = await _unitOfWork.GetRepository<Store>()
            .FirstOrDefaultAsync(x => x.OwnerAccountId == currentUserId && !x.IsDeleted);

        return string.IsNullOrWhiteSpace(store?.RefCode) ? null : store.RefCode;
    }
}
