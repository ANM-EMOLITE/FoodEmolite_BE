using FoodEmolite.Shared.Common;
using System.Text.Json.Serialization;

namespace FoodEmolite.Domain.Enums;

/// <summary>Nguồn đơn — lưu DB / trả API dạng POS | WEB_USER | WEB_GUEST.</summary>
[JsonConverter(typeof(UpperSnakeCaseEnumConverter))]
public enum OrderSource
{
    /// <summary>Chủ cửa hàng tạo tại quầy (POS).</summary>
    Pos,

    /// <summary>User đã đăng nhập đặt trên trang user.</summary>
    WebUser,

    /// <summary>Khách vãng lai đặt qua link cửa hàng.</summary>
    WebGuest
}
