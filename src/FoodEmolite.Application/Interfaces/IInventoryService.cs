using FoodEmolite.Application.DTOs.Inventory;
using FoodEmolite.Domain.Entities;
using FoodEmolite.Domain.Enums;
using FoodEmolite.Shared.Entities;
using FoodEmolite.Shared.Responses;

namespace FoodEmolite.Application.Interfaces;

public interface IInventoryService
{
    Task TrackAsync(StoreFood food, InventoryTransactionType type, int quantityChange, long? actorId, long? orderId = null, string? referenceCode = null, string? note = null);

    Task<BaseTableResponse<InventoryTransactionResponseDto>> SearchTransactionsAsync(long currentUserId, BaseSearchRequest<InventoryTransactionSearchRequest> request);

    Task<BaseTableResponse<InventoryReceiptResponseDto>> SearchReceiptsAsync(long currentUserId, BaseSearchRequest<InventoryDocumentSearchRequest> request);

    Task<BaseResponse<InventoryReceiptResponseDto>> GetReceiptDetailAsync(long currentUserId, long id);

    Task<BaseResponse<string>> CreateReceiptAsync(long currentUserId, CreateInventoryReceiptRequestDto request);

    Task<BaseTableResponse<InventoryStocktakeResponseDto>> SearchStocktakesAsync(long currentUserId, BaseSearchRequest<InventoryDocumentSearchRequest> request);

    Task<BaseResponse<InventoryStocktakeResponseDto>> GetStocktakeDetailAsync(long currentUserId, long id);

    Task<BaseResponse<string>> CreateStocktakeAsync(long currentUserId, CreateInventoryStocktakeRequestDto request);
}
