namespace FoodEmolite.Application.DTOs.Realtime;

public class NewOrderNotificationDto
{
    /// <summary>Id bản ghi StoreNotification đã lưu — FE dùng để đánh dấu đã đọc.</summary>
    public long NotificationId { get; set; }
    public long OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string StoreRefCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}
