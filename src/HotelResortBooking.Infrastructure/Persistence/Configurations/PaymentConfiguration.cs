using HotelResortBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelResortBooking.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments", table =>
        {
            table.HasCheckConstraint("CK_Payments_Amount", "\"Amount\" >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Amount).HasMoneyPrecision().IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.PaymentMethod).HasEnumStringConversion().IsRequired();
        builder.Property(x => x.Status).HasEnumStringConversion().IsRequired();
        builder.Property(x => x.Provider).HasMaxLength(100);
        builder.Property(x => x.ProviderTransactionId).HasMaxLength(200);
        builder.Property(x => x.PaymentMethodToken).HasMaxLength(200);
        builder.Property(x => x.CardBrand).HasMaxLength(50);
        builder.Property(x => x.CardLast4).HasMaxLength(4);
        builder.Property(x => x.FailureReason).HasMaxLength(500);
        builder.Property(x => x.PaidAt).HasColumnType("timestamptz");

        builder.ConfigureAuditableEntity();

        builder.HasIndex(x => x.BookingId);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ProviderTransactionId)
            .IsUnique()
            .HasFilter("\"ProviderTransactionId\" IS NOT NULL");
    }
}
