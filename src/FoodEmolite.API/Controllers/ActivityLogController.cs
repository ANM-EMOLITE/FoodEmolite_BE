using FoodEmolite.Application.DTOs.ActivityLog;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodEmolite.API.Controllers;

[ApiController]
[Authorize]
[Route("api/activity-logs")]
public class ActivityLogController : BaseApiController
{
    private readonly IActivityLogService _activityLogService;

    public ActivityLogController(IActivityLogService activityLogService)
    {
        _activityLogService = activityLogService;
    }

    /// <summary>Lịch sử hoạt động liên quan tới cửa hàng của đại lý đang đăng nhập (không gồm log tạo đơn).</summary>
    [HttpPost("store/search")]
    public async Task<IActionResult> SearchStore([FromBody] BaseSearchRequest<ActivityLogSearchRequest> request)
    {
        var result = await _activityLogService.SearchForAgentStoreAsync(CurrentUserId!.Value, request);

        return Ok(result);
    }
}
