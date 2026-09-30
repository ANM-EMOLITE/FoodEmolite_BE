using FoodEmolite.Domain.Enums;
using FoodEmolite.Shared.Entities;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations.Schema;

namespace FoodEmolite.Domain.Entities;

[Table("orders", Schema = "food_emolite")]
public class Order : BaseEntity
{
    [Column("customer_account_id")]
    public long? CustomerAccountId { get; set; }

    [Column("customer_id")]
    public long? CustomerId { get; set; }

    [Column("order_code")]
    public string OrderCode { get; set; }

    [Column("store_ref_code")]
    public string StoreRefCode { get; set; }

    [Column("total_amount")]
    public decimal TotalAmount { get; set; }

    // Lưu DB dạng PENDING | COMPLETED | CANCELLED
    [Column("order_status")]
    public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;

    // Lưu DB dạng UNPAID | PAID
    [Column("payment_status")]
    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

    // CASH | BANK_TRANSFER
    [Column("payment_method")]
    public string PaymentMethod { get; set; } = "BANK_TRANSFER";

    [Column("note")]
    public string? Note { get; set; }

    [Column("is_delete")]
    public bool IsDelete { get; set; }

    [Column("ip_address")]
    public string? IpAddress { get; set; }

    // BE tự xác định khi tạo đơn, không nhận từ FE — lưu DB dạng POS | WEB_USER | WEB_GUEST
    [Column("order_source")]
    public OrderSource OrderSource { get; set; } = OrderSource.Pos;

    // Đơn Delivery bắt buộc có SĐT + địa chỉ giao hàng — lưu DB dạng DINE_IN | DELIVERY
    [Column("order_type")]
    public OrderType OrderType { get; set; } = OrderType.DineIn;

    [Column("delivery_phone")]
    public string? DeliveryPhone { get; set; }

    // Lưu kèm tên (không chỉ mã) để đơn cũ vẫn hiển thị đúng nếu danh mục hành chính thay đổi
    [Column("delivery_province_code")]
    public string? DeliveryProvinceCode { get; set; }

    [Column("delivery_province_name")]
    public string? DeliveryProvinceName { get; set; }

    [Column("delivery_ward_code")]
    public string? DeliveryWardCode { get; set; }

    [Column("delivery_ward_name")]
    public string? DeliveryWardName { get; set; }

    // Số nhà, tên đường
    [Column("delivery_street")]
    public string? DeliveryStreet { get; set; }

    [Column("delivery_latitude")]
    public decimal? DeliveryLatitude { get; set; }

    [Column("delivery_longitude")]
    public decimal? DeliveryLongitude { get; set; }
}