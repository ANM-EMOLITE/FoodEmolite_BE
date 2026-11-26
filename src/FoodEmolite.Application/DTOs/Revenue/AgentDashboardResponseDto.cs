namespace FoodEmolite.Application.DTOs.Revenue;

public class AgentDashboardResponseDto
{
    public string StoreName { get; set; } = string.Empty;
    public int PendingUnpaidOrders { get; set; }
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public DateTime PreviousFromDate { get; set; }
    public DateTime PreviousToDate { get; set; }
    public DashboardSummaryDto Current { get; set; } = new();
    public DashboardSummaryDto Previous { get; set; } = new();
    public List<DashboardDailyDto> Daily { get; set; } = [];
    public List<DashboardDailyDto> PreviousDaily { get; set; } = [];
    public int OutOfStock { get; set; }
    public int LowStock { get; set; }
    public int Unavailable { get; set; }
    public List<TopSellingProductDto> TopByQuantity { get; set; } = [];
    public List<TopSellingProductDto> TopByRevenue { get; set; } = [];
}

public class DashboardSummaryDto
{
    public int TotalOrders { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal Revenue { get; set; }
    public int PaidOrders { get; set; }
    public int UnpaidOrders { get; set; }
    public decimal UnpaidAmount { get; set; }
    public int CancelledOrders { get; set; }
    public decimal CancelledAmount { get; set; }
    public int Customers { get; set; }
}

public class DashboardDailyDto
{
    public DateTime Date { get; set; }
    public int Orders { get; set; }
    public int Customers { get; set; }
    public decimal CancelledAmount { get; set; }
    public decimal Revenue { get; set; }
    public decimal TotalAmount { get; set; }
}
