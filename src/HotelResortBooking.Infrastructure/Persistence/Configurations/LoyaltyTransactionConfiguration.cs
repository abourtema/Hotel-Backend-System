using HotelResortBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelResortBooking.Infrastructure.Persistence.Configurations;

public sealed class LoyaltyTransactionConfiguration : IEntityTypeConfiguration<LoyaltyTransaction>
{
    public void Configure(EntityTypeBuilder<LoyaltyTransaction> builder)
    {
        builder.ToTable("LoyaltyTransactions", table =>
        {
            table.HasCheckConstraint("CK_LoyaltyTransactions_BalanceAfter", "\"BalanceAfter\" >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TransactionType).HasEnumStringConversion().IsRequired();
        builder.Property(x => x.Points).IsRequired();
        builder.Property(x => x.BalanceAfter).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).ConfigureCreatedAt();

        builder.HasIndex(x => x.LoyaltyAccountId);
        builder.HasIndex(x => x.BookingId);
        builder.HasIndex(x => new { x.LoyaltyAccountId, x.CreatedAt });
    }
}
