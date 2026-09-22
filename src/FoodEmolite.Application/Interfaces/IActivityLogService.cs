using FoodEmolite.Application.DTOs.ActivityLog;
using FoodEmolite.Shared.Entities;
using FoodEmolite.Shared.Responses;

namespace FoodEmolite.Application.Interfaces;

public interface IActivityLogService
{
    Task LogAsync(string actorType, long? actorId, string? actorName, string action, string description, string? storeRefCode = null);

    /// <summary>
    /// Ghi log hành động của đại lý trên cửa hàng của họ. Tự tìm tên hiển thị theo accountId hoặc accountRefCode
    /// (truyền một trong hai).
    /// </summary>
    Task LogAgentActionAsync(long? accountId, string? accountRefCode, string action, string description, string? storeRefCode);

    Task<BaseTableResponse<ActivityLogResponseDto>> SearchAsync(BaseSearchRequest<ActivityLogSearchRequest> request);

    /// <summary>
    /// Lịch sử hoạt động liên quan tới cửa hàng của đại lý đang đăng nhập.
    /// Không bao gồm log tạo đơn hàng (CREATE_ORDER) và log của cửa hàng khác.
    /// </summary>
    Task<BaseTableResponse<ActivityLogResponseDto>> SearchForAgentStoreAsync(long agentAccountId, BaseSearchRequest<ActivityLogSearchRequest> request);
}
