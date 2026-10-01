using FoodEmolite.Application.DTOs.Inventory;
using FoodEmolite.Application.Interfaces;
using FoodEmolite.Shared.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodEmolite.API.Controllers;

[ApiController]
[Authorize]
[Route("api/inventory")]
public class InventoryController : BaseApiController
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpPost("transactions/search")]
    public async Task<IActionResult> SearchTransactions([FromBody] BaseSearchRequest<InventoryTransactionSearchRequest> request)
        => Ok(await _inventoryService.SearchTransactionsAsync(CurrentUserId!.Value, request));

    [HttpPost("receipts/search")]
    public async Task<IActionResult> SearchReceipts([FromBody] BaseSearchRequest<InventoryReceiptSearchRequest> request)
        => Ok(await _inventoryService.SearchReceiptsAsync(CurrentUserId!.Value, request));

    [HttpGet("receipts/{id}")]
    public async Task<IActionResult> GetReceipt(long id)
        => Ok(await _inventoryService.GetReceiptDetailAsync(CurrentUserId!.Value, id));

    [HttpPost("receipts")]
    public async Task<IActionResult> CreateReceipt([FromBody] CreateInventoryReceiptRequestDto request)
        => Ok(await _inventoryService.CreateReceiptAsync(CurrentUserId!.Value, CurrentUserRefCode!, request));

    [HttpPost("stocktakes/search")]
    public async Task<IActionResult> SearchStocktakes([FromBody] BaseSearchRequest<InventoryDocumentSearchRequest> request)
        => Ok(await _inventoryService.SearchStocktakesAsync(CurrentUserId!.Value, request));

    [HttpGet("stocktakes/{id}")]
    public async Task<IActionResult> GetStocktake(long id)
        => Ok(await _inventoryService.GetStocktakeDetailAsync(CurrentUserId!.Value, id));

    [HttpPost("stocktakes")]
    public async Task<IActionResult> CreateStocktake([FromBody] CreateInventoryStocktakeRequestDto request)
        => Ok(await _inventoryService.CreateStocktakeAsync(CurrentUserId!.Value, CurrentUserRefCode!, request));
}
