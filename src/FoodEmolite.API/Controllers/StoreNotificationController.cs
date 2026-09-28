using FoodEmolite.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodEmolite.API.Controllers;

[ApiController]
[Authorize]
[Route("api/store-notifications")]
public class StoreNotificationController : BaseApiController
{
    private readonly IStoreNotificationService _storeNotificationService;

    public StoreNotificationController(IStoreNotificationService storeNotificationService)
    {
        _storeNotificationService = storeNotificationService;
    }

    /// <summary>Thông báo của cửa hàng mà đại lý đang đăng nhập sở hữu, mới nhất trước, kèm số chưa đọc.</summary>
    [HttpGet]
    public async Task<IActionResult> GetMyStore([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool? isRead = null)
    {
        var result = await _storeNotificationService.GetMyStoreAsync(CurrentUserId!.Value, page, pageSize, isRead);

        return Ok(result);
    }

    [HttpPut("{id}/read")]
    public async Task<IActionResult> MarkRead(long id)
    {
        var result = await _storeNotificationService.MarkReadAsync(CurrentUserId!.Value, id);

        return Ok(result);
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var result = await _storeNotificationService.MarkAllReadAsync(CurrentUserId!.Value);

        return Ok(result);
    }
}
