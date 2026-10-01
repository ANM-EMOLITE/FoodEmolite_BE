using FoodEmolite.Application.DTOs.Supplier;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodEmolite.API.Controllers;

[ApiController]
[Authorize]
[Route("api/suppliers")]
public class SupplierController : BaseApiController
{
    private readonly ISupplierService _supplierService;

    public SupplierController(ISupplierService supplierService)
    {
        _supplierService = supplierService;
    }

    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] BaseSearchRequest<SupplierSearchRequest> request)
        => Ok(await _supplierService.SearchAsync(CurrentUserId!.Value, request));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetDetail(long id)
        => Ok(await _supplierService.GetDetailAsync(CurrentUserId!.Value, id));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] SaveSupplierRequestDto request)
        => Ok(await _supplierService.CreateAsync(CurrentUserId!.Value, CurrentUserRefCode!, request));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(long id, [FromBody] SaveSupplierRequestDto request)
        => Ok(await _supplierService.UpdateAsync(CurrentUserId!.Value, id, request));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
        => Ok(await _supplierService.DeleteAsync(CurrentUserId!.Value, id));
}
