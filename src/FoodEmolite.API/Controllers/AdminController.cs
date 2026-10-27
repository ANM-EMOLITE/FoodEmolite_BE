using FoodEmolite.Application.DTOs.ActivityLog;
using FoodEmolite.Application.DTOs.Admin;
using FoodEmolite.Application.DTOs.Auth;
using FoodEmolite.Application.DTOs.Customer;
using FoodEmolite.Application.DTOs.Order;
using FoodEmolite.Application.DTOs.Revenue;
using FoodEmolite.Application.DTOs.Store;
using FoodEmolite.Application.ExternalService.Interfaces;
using FoodEmolite.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodEmolite.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    [HttpPost("stores")]
    public async Task<IActionResult> CreateStore([FromForm] CreateStoreRequestDto request)
    {
        var result = await _adminService.CreateStoreAsync(request);
        return Ok(result);
    }

    [HttpPut("stores/{id}")]
    public async Task<IActionResult> UpdateStore(long id, [FromForm] UpdateStoreRequestDto request)
    {
        var result = await _adminService.UpdateStoreAsync(id, request);
        return Ok(result);
    }

    [HttpPut("stores/{id}/approve")]
    public async Task<IActionResult> ApproveStore(long id)
        => Ok(await _adminService.SetStoreApprovalAsync(id, true));

    [HttpPut("stores/{id}/reject")]
    public async Task<IActionResult> RejectStore(long id)
        => Ok(await _adminService.SetStoreApprovalAsync(id, false));

    [HttpDelete("stores/{id}")]
    public async Task<IActionResult> DeleteStore(long id)
    {
        var result = await _adminService.DeleteStoreAsync(id);
        return Ok(result);
    }

    [HttpPost("stores/search")]
    public async Task<IActionResult> SearchStores([FromBody] BaseSearchRequest<AdminStoreSearchRequest> request)
    {
        var result = await _adminService.SearchStoresAsync(request);
        return Ok(result);
    }

    [HttpGet("stores/{id}")]
    public async Task<IActionResult> GetStoreDetail(long id)
    {
        var result = await _adminService.GetStoreDetailAsync(id);
        return Ok(result);
    }

    [HttpPost("stores/owner/search")]
    public async Task<IActionResult> SearchStoresByOwner([FromBody] BaseSearchRequest<AdminStoreByOwnerSearchRequest> request)
    {
        var result = await _adminService.SearchStoresByOwnerAsync(request);
        return Ok(result);
    }

    [HttpPost("users/search")]
    public async Task<IActionResult> SearchUsers([FromBody] BaseSearchRequest<AdminUserSearchRequest> request)
    {
        var result = await _adminService.SearchUsersAsync(request);
        return Ok(result);
    }

    [HttpPost("agents/search")]
    public async Task<IActionResult> SearchAgents([FromBody] BaseSearchRequest<AdminAgentSearchRequest> request)
    {
        var result = await _adminService.SearchAgentsAsync(request);
        return Ok(result);
    }

    [HttpPost("agents")]
    public async Task<IActionResult> CreateAgent(RegisterRequest request)
    {
        var result = await _adminService.CreateAgentAsync(request);
        return Ok(result);
    }

    [HttpGet("revenue")]
    public async Task<IActionResult> GetRevenue([FromQuery] RevenueQueryDto request)
    {
        var result = await _adminService.GetRevenueAsync(request);
        return Ok(result);
    }

    [HttpGet("revenue/top-products")]
    public async Task<IActionResult> GetTopProducts([FromQuery] RevenueQueryDto request, [FromQuery] string? storeRefCode, [FromQuery] int top = 10)
    {
        var result = await _adminService.GetTopProductsAsync(request, storeRefCode, top);
        return Ok(result);
    }

    [HttpPost("revenue/products/search")]
    public async Task<IActionResult> SearchProductRevenue([FromBody] BaseSearchRequest<ProductRevenueSearchRequest> request)
    {
        var result = await _adminService.SearchProductRevenueAsync(request);
        return Ok(result);
    }

    [HttpPost("customers/search")]
    public async Task<IActionResult> SearchCustomers([FromBody] BaseSearchRequest<CustomerSearchRequest> request)
    {
        var result = await _adminService.SearchCustomersAsync(request);
        return Ok(result);
    }

    [HttpPost("store-foods/search")]
    public async Task<IActionResult> SearchStoreFoods([FromBody] BaseSearchRequest<AdminStoreFoodSearchRequest> request)
    {
        var result = await _adminService.SearchStoreFoodsAsync(request);
        return Ok(result);
    }

    [HttpPost("categories/search")]
    public async Task<IActionResult> SearchCategories([FromBody] BaseSearchRequest<AdminCategorySearchRequest> request)
    {
        var result = await _adminService.SearchCategoriesAsync(request);
        return Ok(result);
    }

    [HttpPost("orders/search")]
    public async Task<IActionResult> SearchOrders([FromBody] BaseSearchRequest<OrderSearchRequest> request)
    {
        var result = await _adminService.SearchOrdersAsync(request);
        return Ok(result);
    }

    [HttpPost("activity-logs/search")]
    public async Task<IActionResult> SearchActivityLogs([FromBody] BaseSearchRequest<ActivityLogSearchRequest> request)
    {
        var result = await _adminService.SearchActivityLogsAsync(request);
        return Ok(result);
    }
}
