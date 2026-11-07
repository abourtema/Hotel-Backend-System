using HotelResortBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelResortBooking.Infrastructure.Persistence.Configurations;

public sealed class BookingUpgradeConfiguration : IEntityTypeConfiguration<BookingUpgrade>
{
    public void Configure(EntityTypeBuilder<BookingUpgrade> builder)
    {
        builder.ToTable("BookingUpgrades", table =>
        {
            table.HasCheckConstraint("CK_BookingUpgrades_Quantity", "\"Quantity\" > 0");
            table.HasCheckConstraint("CK_BookingUpgrades_UnitPrice", "\"UnitPrice\" >= 0");
            table.HasCheckConstraint("CK_BookingUpgrades_TotalPrice", "\"TotalPrice\" >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.UnitPrice).HasMoneyPrecision().IsRequired();
        builder.Property(x => x.TotalPrice).HasMoneyPrecision().IsRequired();
        builder.Property(x => x.CreatedAt).ConfigureCreatedAt();

        builder.HasIndex(x => x.BookingId);
        builder.HasIndex(x => x.UpgradeOptionId);
    }
}
