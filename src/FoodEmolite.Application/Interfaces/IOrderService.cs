using FoodEmolite.Application.DTOs.Order;
using FoodEmolite.Shared.Entities;
using FoodEmolite.Shared.Responses;

namespace FoodEmolite.Application.Interfaces;

public interface IOrderService
{
    Task<BaseResponse<CreateOrderResponseDto>> CreateAsync(long currentUserId, string refCode, CreateOrderRequestDto request);
    Task<BaseResponse<CreateOrderResponseDto>> CreateGuestAsync(CreateGuestOrderRequestDto request);
    Task<BaseTableResponse<OrderResponseDto>> GetMyOrdersAsync(long currentUserId, int page, int pageSize);

    Task<BaseResponse<OrderResponseDto>> GetDetailAsync(long id, long currentUserId);

    /// <summary>
    /// Chi tiết đơn hàng cho đại lý — lọc theo cửa hàng do currentUserId sở hữu (Store.OwnerAccountId), không phải CustomerAccountId.
    /// Không dùng claim "ref_code" trên JWT vì đó là RefCode của account, không phải RefCode của cửa hàng.
    /// </summary>
    Task<BaseResponse<OrderResponseDto>> GetDetailByStoreAsync(long id, long currentUserId);

    Task<BaseTableResponse<OrderResponseDto>> GetByStoreRefCodeAsync(BaseSearchRequest<OrderSearchRequest> request);

    /// <summary>Danh sách đơn hàng toàn hệ thống (mọi cửa hàng) — dùng cho admin. StoreRefCode trong SearchParams là lọc tuỳ chọn.</summary>
    Task<BaseTableResponse<OrderResponseDto>> GetAllForAdminAsync(BaseSearchRequest<OrderSearchRequest> request);

    Task<BaseResponse<string>> UpdateStatusAsync(long id, long currentUserId, string refCode, UpdateOrderStatusRequestDto request);

    Task<BaseResponse<string>> UpdatePaymentStatusAsync(long id, long currentUserId, string refCode, UpdatePaymentStatusRequestDto request);

    Task<BaseResponse<string>> CancelAsync(long id, long currentUserId, string refCode);


    Task<BaseResponse<string>> GetPaymentStatusAsync(string orderCode);

    Task<BaseResponse<string?>> CheckPendingOrderAsync(string deviceId);
}