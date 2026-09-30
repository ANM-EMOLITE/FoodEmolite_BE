using FoodEmolite.Shared.Common;
using System.Text.Json.Serialization;

namespace FoodEmolite.Domain.Enums;

[JsonConverter(typeof(UpperSnakeCaseEnumConverter))]
public enum PaymentStatus
{
    Unpaid,

    Paid
}
