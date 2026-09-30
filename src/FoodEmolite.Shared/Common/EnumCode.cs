using System.Text.Json;
using System.Text.Json.Serialization;

namespace FoodEmolite.Shared.Common;

public static class EnumCode
{
    public static string ToCode<TEnum>(TEnum value) where TEnum : struct, Enum
        => JsonNamingPolicy.SnakeCaseUpper.ConvertName(value.ToString());

    public static TEnum Parse<TEnum>(string code) where TEnum : struct, Enum
        => Enum.GetValues<TEnum>().First(x => ToCode(x) == code);

    public static bool TryParse<TEnum>(string? code, out TEnum value) where TEnum : struct, Enum
    {
        foreach (var item in Enum.GetValues<TEnum>())
        {
            if (ToCode(item) == code)
            {
                value = item;
                return true;
            }
        }

        value = default;
        return false;
    }
}

public class UpperSnakeCaseEnumConverter : JsonStringEnumConverter
{
    public UpperSnakeCaseEnumConverter() : base(JsonNamingPolicy.SnakeCaseUpper, allowIntegerValues: false)
    {
    }
}
