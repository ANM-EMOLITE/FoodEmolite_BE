using FoodEmolite.Shared.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

[Table("inventory_stocktakes", Schema = "food_emolite")]
public class InventoryStocktake : BaseEntity
{
    [Column("store_ref_code")]
    public string StoreRefCode { get; set; } = string.Empty;

    [Column("stocktake_code")]
    public string StocktakeCode { get; set; } = string.Empty;

    [Column("note")]
    public string? Note { get; set; }

    [Column("total_items")]
    public int TotalItems { get; set; }

    [Column("total_difference")]
    public int TotalDifference { get; set; }

    [Column("difference_value")]
    public decimal DifferenceValue { get; set; }
}

[Table("inventory_stocktake_items", Schema = "food_emolite")]
public class InventoryStocktakeItem : BaseEntity
{
    [Column("stocktake_id")]
    public long StocktakeId { get; set; }

    [Column("store_food_id")]
    public long StoreFoodId { get; set; }

    [Column("system_quantity")]
    public int SystemQuantity { get; set; }

    [Column("actual_quantity")]
    public int ActualQuantity { get; set; }

    [Column("difference")]
    public int Difference { get; set; }

    [Column("unit_cost")]
    public decimal UnitCost { get; set; }

    [Column("note")]
    public string? Note { get; set; }
}
