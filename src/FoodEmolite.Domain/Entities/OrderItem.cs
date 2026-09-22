using FoodEmolite.Shared.Entities;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

[Table("order_items", Schema = "food_emolite")]
public class OrderItem : BaseEntity
{
    [Column("order_id")]
    public long OrderId { get; set; }

    [Column("store_food_id")]
    public long StoreFoodId { get; set; }

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("unit_price")]
    public decimal UnitPrice { get; set; }

    [Column("total_price")]
    public decimal TotalPrice { get; set; }

    /// <summary>Đơn giá GỐC (chưa áp khuyến mãi) tại thời điểm đặt hàng — dùng để tính số tiền đã giảm, không đổi theo giá món hiện tại.</summary>
    [Column("original_unit_price")]
    public decimal OriginalUnitPrice { get; set; }

    /// <summary>Khuyến mãi đã áp cho dòng này (nếu có) — null nếu không có khuyến mãi.</summary>
    [Column("promotion_id")]
    public long? PromotionId { get; set; }

    /// <summary>Snapshot tên khuyến mãi tại thời điểm đặt hàng, để hiển thị đúng dù chương trình sau này bị sửa/xoá.</summary>
    [Column("promotion_name")]
    public string? PromotionName { get; set; }
}