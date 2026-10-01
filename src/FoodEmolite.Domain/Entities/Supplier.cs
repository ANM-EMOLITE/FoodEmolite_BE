using FoodEmolite.Shared.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

[Table("suppliers", Schema = "food_emolite")]
public class Supplier : BaseEntity
{
    [Column("store_ref_code")]
    public string StoreRefCode { get; set; } = string.Empty;

    [Column("supplier_code")]
    public string SupplierCode { get; set; } = string.Empty;

    [Column("supplier_name")]
    public string SupplierName { get; set; } = string.Empty;

    [Column("contact_name")]
    public string? ContactName { get; set; }

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("email")]
    public string? Email { get; set; }

    [Column("address")]
    public string? Address { get; set; }

    [Column("tax_code")]
    public string? TaxCode { get; set; }

    [Column("note")]
    public string? Note { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("is_deleted")]
    public bool IsDeleted { get; set; } = false;
}
