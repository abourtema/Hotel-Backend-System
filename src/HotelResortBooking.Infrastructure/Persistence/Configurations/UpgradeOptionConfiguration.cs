using HotelResortBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelResortBooking.Infrastructure.Persistence.Configurations;

public sealed class UpgradeOptionConfiguration : IEntityTypeConfiguration<UpgradeOption>
{
    public void Configure(EntityTypeBuilder<UpgradeOption> builder)
    {
        builder.ToTable("UpgradeOptions", table =>
        {
            table.HasCheckConstraint("CK_UpgradeOptions_Price", "\"Price\" >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasColumnType("text");
        builder.Property(x => x.UpgradeType).HasEnumStringConversion().IsRequired();
        builder.Property(x => x.Price).HasMoneyPrecision().IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.IsMandatory).IsRequired();
        builder.Property(x => x.IsRecommended).IsRequired();
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.ConfigureAuditableEntity();

        builder.HasIndex(x => x.PropertyId);
        builder.HasIndex(x => new { x.PropertyId, x.IsActive });

        builder.HasMany(x => x.RoomTypeUpgradeOptions)
            .WithOne(x => x.UpgradeOption)
            .HasForeignKey(x => x.UpgradeOptionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.BookingUpgrades)
            .WithOne(x => x.UpgradeOption)
            .HasForeignKey(x => x.UpgradeOptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
