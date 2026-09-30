namespace FoodEmolite.Application.DTOs.Inventory;

public class CreateInventoryStocktakeRequestDto
{
    public string? Note { get; set; }
    public List<CreateInventoryStocktakeItemDto> Items { get; set; } = [];
}

public class CreateInventoryStocktakeItemDto
{
    public long StoreFoodId { get; set; }
    public int ActualQuantity { get; set; }
    public string? Note { get; set; }
}

public class InventoryStocktakeResponseDto
{
    public long Id { get; set; }
    public string StocktakeCode { get; set; } = string.Empty;
    public string? Note { get; set; }
    public int TotalItems { get; set; }
    public int TotalDifference { get; set; }
    public decimal DifferenceValue { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ActorName { get; set; }
    public List<InventoryStocktakeItemResponseDto> Items { get; set; } = [];
}

public class InventoryStocktakeItemResponseDto
{
    public long StoreFoodId { get; set; }
    public string FoodName { get; set; } = string.Empty;
    public string ProductCode { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int SystemQuantity { get; set; }
    public int ActualQuantity { get; set; }
    public int Difference { get; set; }
    public decimal UnitCost { get; set; }
    public string? Note { get; set; }
}
