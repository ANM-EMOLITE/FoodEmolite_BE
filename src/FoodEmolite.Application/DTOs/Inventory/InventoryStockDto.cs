namespace FoodEmolite.Application.DTOs.Inventory;

public class InventoryStockSearchRequest
{
    public string? Keyword { get; set; }
    public long? StoreFoodCategoryId { get; set; }
    public string? StockStatus { get; set; }
}

public class InventoryStockItemDto
{
    public long StoreFoodId { get; set; }
    public string FoodName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? CategoryName { get; set; }
    public int Quantity { get; set; }
    public decimal CostPrice { get; set; }
    public decimal Price { get; set; }
    public decimal StockValue { get; set; }
    public string StockStatus { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
}

public class InventoryStockSummaryDto
{
    public int TotalItems { get; set; }
    public int TotalQuantity { get; set; }
    public decimal TotalStockValue { get; set; }
    public int OutOfStock { get; set; }
    public int LowStock { get; set; }
}
