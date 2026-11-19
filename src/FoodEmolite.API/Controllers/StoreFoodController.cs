using FoodEmolite.Application.DTOs.StoreFood;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodEmolite.API.Controllers;

[ApiController]
[Authorize]
[Route("api/store-foods")]
public class StoreFoodController : BaseApiController
{
    private readonly IStoreFoodService _storeFoodService;

    public StoreFoodController(IStoreFoodService storeFoodService)
    {
        _storeFoodService = storeFoodService;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateStoreFoodRequestDto request)
    {
        var result = await _storeFoodService.CreateAsync(CurrentUserRefCode, request);
        return Ok(result);
    }

    [HttpGet("check-product-code")]
    public async Task<IActionResult> CheckProductCode([FromQuery] string productCode, [FromQuery] long? excludeId)
    {
        var result = await _storeFoodService.CheckProductCodeExistsAsync(CurrentUserRefCode, productCode, excludeId);
        return Ok(result);
    }

    [HttpPost("bulk")]
    public async Task<IActionResult> CreateBulk([FromBody] CreateBulkStoreFoodRequestDto request)
    {
        var result = await _storeFoodService.CreateBulkAsync(CurrentUserRefCode, request);
        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, UpdateStoreFoodRequestDto request)
    {
        var result = await _storeFoodService.UpdateAsync(CurrentUserRefCode, id, request);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var result = await _storeFoodService.DeleteAsync(CurrentUserRefCode, id);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetail(long id)
    {
        var result = await _storeFoodService.GetDetailAsync(id, CurrentUserId);
        return Ok(result);
    }

    [AllowAnonymous]
    [HttpPost("store")]
    public async Task<IActionResult> GetByStoreRefCode([FromBody] BaseSearchRequest<GetStoreFoodsRequest> request)
    {
        var result = await _storeFoodService.GetByStoreRefCodeAsync(request, CurrentUserId);

        return Ok(result);
    }
}