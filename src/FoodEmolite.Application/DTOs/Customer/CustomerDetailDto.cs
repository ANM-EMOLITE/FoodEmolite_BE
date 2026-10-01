using FoodEmolite.Domain.Enums;

namespace FoodEmolite.Application.DTOs.Customer;

public class CustomerDetailDto
{
    public string RefCode { get; set; } = string.Empty;
    public string? CustomerCode { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Gender { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Address { get; set; }
    public bool IsGuest { get; set; }
    public int TotalOrders { get; set; }
    public int PaidOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime? FirstOrderAt { get; set; }
    public DateTime? LastOrderAt { get; set; }
    public List<CustomerRecentOrderDto> RecentOrders { get; set; } = [];
}

public class CustomerRecentOrderDto
{
    public long Id { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public OrderStatus OrderStatus { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public OrderType OrderType { get; set; }
    public OrderSource OrderSource { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAt { get; set; }
}
