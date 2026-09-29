using System.Text.Json;
using System.Text.Json.Serialization;

namespace FoodEmolite.Shared.Common;

/// <summary>
/// Quy ước lưu enum dạng chuỗi UPPER_SNAKE_CASE (vd: WebGuest &lt;-&gt; "WEB_GUEST") — dùng chung cho DB (EF value converter) và JSON.
/// </summary>
public static class EnumCode
{
    public static string ToCode<TEnum>(TEnum value) where TEnum : struct, Enum
        => JsonNamingPolicy.SnakeCaseUpper.ConvertName(value.ToString());

    public static TEnum Parse<TEnum>(string code) where TEnum : struct, Enum
        => Enum.GetValues<TEnum>().First(x => ToCode(x) == code);
}

/// <summary>Gắn lên enum để API trả / nhận chuỗi UPPER_SNAKE_CASE thay vì số.</summary>
public class UpperSnakeCaseEnumConverter : JsonStringEnumConverter
{
    public UpperSnakeCaseEnumConverter() : base(JsonNamingPolicy.SnakeCaseUpper, allowIntegerValues: false)
    {
    }
}
