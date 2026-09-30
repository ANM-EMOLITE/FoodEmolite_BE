using FoodEmolite.Shared.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

[Table("inventory_receipts", Schema = "food_emolite")]
public class InventoryReceipt : BaseEntity
{
    [Column("store_ref_code")]
    public string StoreRefCode { get; set; } = string.Empty;

    [Column("receipt_code")]
    public string ReceiptCode { get; set; } = string.Empty;

    [Column("supplier_name")]
    public string? SupplierName { get; set; }

    [Column("note")]
    public string? Note { get; set; }

    [Column("total_quantity")]
    public int TotalQuantity { get; set; }

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }
}

[Table("inventory_receipt_items", Schema = "food_emolite")]
public class InventoryReceiptItem : BaseEntity
{
    [Column("receipt_id")]
    public long ReceiptId { get; set; }

    [Column("store_food_id")]
    public long StoreFoodId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("unit_cost")]
    public decimal UnitCost { get; set; }

    [Column("total_cost")]
    public decimal TotalCost { get; set; }
}
