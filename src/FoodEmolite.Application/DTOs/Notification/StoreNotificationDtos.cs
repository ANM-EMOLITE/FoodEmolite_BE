namespace FoodEmolite.Application.DTOs.Notification;

public class StoreNotificationResponseDto
{
    public long Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public long? OrderId { get; set; }
    public string? OrderCode { get; set; }
    public string StoreRefCode { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public decimal? TotalAmount { get; set; }
    public long? StoreFoodId { get; set; }
    public string? FoodName { get; set; }
    public int? Quantity { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class StoreNotificationListResponseDto
{
    public List<StoreNotificationResponseDto> Items { get; set; } = [];

    /// <summary>Tổng số thông báo chưa đọc của cửa hàng (không chỉ trong trang đang lấy).</summary>
    public int UnreadCount { get; set; }

    public int OrderUnreadCount { get; set; }

    public int StockUnreadCount { get; set; }

    public int TotalRecords { get; set; }
}

public class StockAlertNotificationDto
{
    public string StoreRefCode { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public long StoreFoodId { get; set; }
    public string FoodName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime CreatedAt { get; set; }
}
