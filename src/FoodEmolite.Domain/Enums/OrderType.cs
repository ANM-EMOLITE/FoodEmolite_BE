using FoodEmolite.Shared.Common;
using System.Text.Json.Serialization;

namespace FoodEmolite.Domain.Enums;

/// <summary>Loại đơn — lưu DB / trả API dạng DINE_IN | DELIVERY.</summary>
[JsonConverter(typeof(UpperSnakeCaseEnumConverter))]
public enum OrderType
{
    /// <summary>Tại quầy.</summary>
    DineIn,

    /// <summary>Giao hàng — bắt buộc có SĐT + địa chỉ giao hàng.</summary>
    Delivery
}
