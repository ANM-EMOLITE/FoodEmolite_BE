using FoodEmolite.API.Filters;
using FoodEmolite.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodEmolite.API.Controllers;

[ApiController]
[Authorize]
[AgentValidation]
[Route("api/store-notifications")]
public class StoreNotificationController : BaseApiController
{
    private readonly IStoreNotificationService _storeNotificationService;

    public StoreNotificationController(IStoreNotificationService storeNotificationService)
    {
        _storeNotificationService = storeNotificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyStore([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool? isRead = null, [FromQuery] int? days = null)
    {
        var result = await _storeNotificationService.GetMyStoreAsync(CurrentUserId!.Value, page, pageSize, isRead, days);

        return Ok(result);
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkRead(long id)
    {
        var result = await _storeNotificationService.MarkReadAsync(CurrentUserId!.Value, id);

        return Ok(result);
    }

    [HttpGet("group")]
    public async Task<IActionResult> GetMyStoreByGroup([FromQuery] string? group = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool? isRead = null, [FromQuery] int? days = null)
    {
        var result = await _storeNotificationService.GetMyStoreByGroupAsync(CurrentUserId!.Value, page, pageSize, group, isRead, days);

        return Ok(result);
    }

    [HttpPut("read-all/{group}")]
    public async Task<IActionResult> MarkGroupRead(string group)
    {
        var result = await _storeNotificationService.MarkGroupReadAsync(CurrentUserId!.Value, group);

        return Ok(result);
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var result = await _storeNotificationService.MarkAllReadAsync(CurrentUserId!.Value);

        return Ok(result);
    }
}
