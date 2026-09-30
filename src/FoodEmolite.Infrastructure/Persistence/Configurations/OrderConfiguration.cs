using FoodEmolite.Domain.Entities;
using FoodEmolite.Domain.Enums;
using FoodEmolite.Shared.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FoodEmolite.Infrastructure.Persistence.Configurations;

public class OrderConfiguration
    : IEntityTypeConfiguration<Order>
{
    public void Configure(
        EntityTypeBuilder<Order> builder)
    {
        builder.Property(x => x.OrderSource)
            .HasConversion(
                v => EnumCode.ToCode(v),
                v => EnumCode.Parse<OrderSource>(v));

        builder.Property(x => x.OrderType)
            .HasConversion(
                v => EnumCode.ToCode(v),
                v => EnumCode.Parse<OrderType>(v));

        builder.Property(x => x.OrderStatus)
            .HasConversion(
                v => EnumCode.ToCode(v),
                v => EnumCode.Parse<OrderStatus>(v));

        builder.Property(x => x.PaymentStatus)
            .HasConversion(
                v => EnumCode.ToCode(v),
                v => EnumCode.Parse<PaymentStatus>(v));
    }
}
