namespace FoodEmolite.Application.DTOs.Supplier;

public class SupplierSearchRequest
{
    public string? Keyword { get; set; }
    public bool? IsActive { get; set; }
}

public class SaveSupplierRequestDto
{
    public string SupplierName { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
    public string? Note { get; set; }
    public bool IsActive { get; set; } = true;
}

public class SupplierResponseDto
{
    public long Id { get; set; }
    public string SupplierCode { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxCode { get; set; }
    public string? Note { get; set; }
    public bool IsActive { get; set; }
    public int TotalReceipts { get; set; }
    public int TotalQuantity { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime? LastReceiptAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
