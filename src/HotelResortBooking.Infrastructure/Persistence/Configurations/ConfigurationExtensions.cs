using HotelResortBooking.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelResortBooking.Infrastructure.Persistence.Configurations;

internal static class ConfigurationExtensions
{
    public static PropertyBuilder<decimal> HasMoneyPrecision(this PropertyBuilder<decimal> builder)
        => builder.HasPrecision(18, 2);

    public static PropertyBuilder<decimal?> HasMoneyPrecision(this PropertyBuilder<decimal?> builder)
        => builder.HasPrecision(18, 2);

    public static void ConfigureAuditableEntity<TEntity>(this EntityTypeBuilder<TEntity> builder)
        where TEntity : AuditableEntity
    {
        builder.Property(e => e.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnType("timestamptz").IsRequired();
    }

    public static void ConfigureCreatedAt(this PropertyBuilder<DateTimeOffset> builder)
        => builder.HasColumnType("timestamptz").IsRequired();

    public static PropertyBuilder<TEnum> HasEnumStringConversion<TEnum>(this PropertyBuilder<TEnum> builder, int maxLength = 64)
        where TEnum : struct, Enum
        => builder.HasConversion<string>().HasMaxLength(maxLength);

    public static PropertyBuilder<TEnum?> HasEnumStringConversion<TEnum>(this PropertyBuilder<TEnum?> builder, int maxLength = 64)
        where TEnum : struct, Enum
        => builder.HasConversion<string>().HasMaxLength(maxLength);
}
