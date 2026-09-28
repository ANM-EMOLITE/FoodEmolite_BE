using FoodEmolite.Shared.Entities;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

/// <summary>
/// Thông báo gửi cho chủ cửa hàng (chuông trên topbar agent) — lưu lại để xem được cả khi reload
/// hoặc khi đơn đến lúc chủ cửa hàng không mở web. Realtime vẫn đẩy qua SignalR như cũ.
/// </summary>
[Table("store_notifications", Schema = "food_emolite")]
public class StoreNotification : BaseEntity
{
    [Column("store_ref_code")]
    public string StoreRefCode { get; set; } = string.Empty;

    /// <summary>Loại thông báo: NEW_ORDER (hiện tại chỉ có đơn hàng mới).</summary>
    [Column("type")]
    public string Type { get; set; } = "NEW_ORDER";

    [Column("order_id")]
    public long? OrderId { get; set; }

    [Column("order_code")]
    public string? OrderCode { get; set; }

    [Column("customer_name")]
    public string? CustomerName { get; set; }

    [Column("total_amount")]
    public decimal? TotalAmount { get; set; }

    [Column("is_read")]
    public bool IsRead { get; set; } = false;

    [Column("read_at")]
    public DateTime? ReadAt { get; set; }
}
