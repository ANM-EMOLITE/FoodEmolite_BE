using FoodEmolite.Domain.Enums;

namespace FoodEmolite.Application.DTOs.Inventory;

public class InventoryTransactionSearchRequest
{
    public string? Keyword { get; set; }
    public long? StoreFoodId { get; set; }
    public InventoryTransactionType? Type { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class InventoryTransactionResponseDto
{
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public long StoreFoodId { get; set; }
    public string FoodName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public InventoryTransactionType Type { get; set; }
    public int QuantityChange { get; set; }
    public int QuantityAfter { get; set; }
    public decimal? UnitCost { get; set; }
    public long? OrderId { get; set; }
    public string? ReferenceCode { get; set; }
    public string? Note { get; set; }
    public string? ActorName { get; set; }
}
