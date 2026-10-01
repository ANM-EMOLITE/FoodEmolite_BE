namespace FoodEmolite.Application.DTOs.Inventory;

public class InventoryDocumentSearchRequest
{
    public string? Keyword { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class InventoryReceiptSearchRequest : InventoryDocumentSearchRequest
{
    public long? SupplierId { get; set; }
}

public class CreateInventoryReceiptRequestDto
{
    public long? SupplierId { get; set; }
    public string? Note { get; set; }
    public List<CreateInventoryReceiptItemDto> Items { get; set; } = [];
}

public class CreateInventoryReceiptItemDto
{
    public long StoreFoodId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
}

public class InventoryReceiptResponseDto
{
    public long Id { get; set; }
    public string ReceiptCode { get; set; } = string.Empty;
    public long? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string? Note { get; set; }
    public int TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
    public int TotalItems { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ActorName { get; set; }
    public List<InventoryReceiptItemResponseDto> Items { get; set; } = [];
}

public class InventoryReceiptItemResponseDto
{
    public long StoreFoodId { get; set; }
    public string FoodName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
}
