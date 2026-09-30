using FoodEmolite.Shared.Common;
using System.Text.Json.Serialization;

namespace FoodEmolite.Domain.Enums;

[JsonConverter(typeof(UpperSnakeCaseEnumConverter))]
public enum OrderStatus
{
    Pending,
    Confirmed,
    Completed,
    Cancelled
}
