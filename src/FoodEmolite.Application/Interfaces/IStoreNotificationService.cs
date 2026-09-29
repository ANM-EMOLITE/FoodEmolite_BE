using FoodEmolite.Application.DTOs.Notification;
using FoodEmolite.Domain.Entities;
using FoodEmolite.Shared.Responses;

namespace FoodEmolite.Application.Interfaces;

public interface IStoreNotificationService
{
    /// <summary>Lưu thông báo đơn hàng mới cho cửa hàng của đơn — trả về bản ghi đã lưu (có Id) để đẩy realtime kèm theo.</summary>
    Task<StoreNotification> CreateNewOrderAsync(Order order, string customerName);

    /// <summary>Thông báo của cửa hàng mà đại lý đang đăng nhập sở hữu, mới nhất trước. isRead: null = tất cả.</summary>
    /// <param name="days">Chỉ lấy thông báo trong N ngày gần nhất (null = tất cả).</param>
    Task<BaseResponse<StoreNotificationListResponseDto>> GetMyStoreAsync(long currentUserId, int page, int pageSize, bool? isRead = null, int? days = null);

    Task<BaseResponse<string>> MarkReadAsync(long currentUserId, long id);

    Task<BaseResponse<string>> MarkAllReadAsync(long currentUserId);
}
