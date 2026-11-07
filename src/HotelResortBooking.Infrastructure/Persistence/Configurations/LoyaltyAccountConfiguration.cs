using HotelResortBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelResortBooking.Infrastructure.Persistence.Configurations;

public sealed class LoyaltyAccountConfiguration : IEntityTypeConfiguration<LoyaltyAccount>
{
    public void Configure(EntityTypeBuilder<LoyaltyAccount> builder)
    {
        builder.ToTable("LoyaltyAccounts", table =>
        {
            table.HasCheckConstraint("CK_LoyaltyAccounts_CurrentBalance", "\"CurrentBalance\" >= 0");
            table.HasCheckConstraint("CK_LoyaltyAccounts_LifetimeEarned", "\"LifetimeEarned\" >= 0");
            table.HasCheckConstraint("CK_LoyaltyAccounts_LifetimeRedeemed", "\"LifetimeRedeemed\" >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CurrentBalance).IsRequired();
        builder.Property(x => x.LifetimeEarned).IsRequired();
        builder.Property(x => x.LifetimeRedeemed).IsRequired();

        builder.ConfigureAuditableEntity();

        builder.HasIndex(x => x.UserId).IsUnique();

        builder.HasMany(x => x.Transactions)
            .WithOne(x => x.LoyaltyAccount)
            .HasForeignKey(x => x.LoyaltyAccountId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
