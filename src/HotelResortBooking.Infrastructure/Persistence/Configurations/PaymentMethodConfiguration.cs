using HotelResortBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelResortBooking.Infrastructure.Persistence.Configurations;

public sealed class PaymentMethodConfiguration : IEntityTypeConfiguration<PaymentMethod>
{
    public void Configure(EntityTypeBuilder<PaymentMethod> builder)
    {
        builder.ToTable("PaymentMethods", table =>
        {
            table.HasCheckConstraint(
                "CK_PaymentMethods_ExpiryMonth",
                "\"ExpiryMonth\" IS NULL OR (\"ExpiryMonth\" >= 1 AND \"ExpiryMonth\" <= 12)");
            table.HasCheckConstraint(
                "CK_PaymentMethods_ExpiryYear",
                "\"ExpiryYear\" IS NULL OR \"ExpiryYear\" >= 2000");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Provider).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ProviderPaymentMethodId).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CardBrand).HasMaxLength(50);
        builder.Property(x => x.CardLast4).HasMaxLength(4);
        builder.Property(x => x.IsDefault).IsRequired();
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.ConfigureAuditableEntity();

        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => new { x.UserId, x.IsDefault });
        builder.HasIndex(x => new { x.Provider, x.ProviderPaymentMethodId }).IsUnique();
    }
}
