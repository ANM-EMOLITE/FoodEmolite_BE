using FoodEmolite.Domain.Enums;
using FoodEmolite.Shared.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

[Table("inventory_transactions", Schema = "food_emolite")]
public class InventoryTransaction : BaseEntity
{
    [Column("store_ref_code")]
    public string StoreRefCode { get; set; } = string.Empty;

    [Column("store_food_id")]
    public long StoreFoodId { get; set; }

    [Column("type")]
    public InventoryTransactionType Type { get; set; }

    [Column("quantity_change")]
    public int QuantityChange { get; set; }

    [Column("quantity_after")]
    public int QuantityAfter { get; set; }

    [Column("unit_cost")]
    public decimal? UnitCost { get; set; }

    [Column("order_id")]
    public long? OrderId { get; set; }

    [Column("reference_code")]
    public string? ReferenceCode { get; set; }

    [Column("note")]
    public string? Note { get; set; }
}
