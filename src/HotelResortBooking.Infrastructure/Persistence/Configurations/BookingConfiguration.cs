using HotelResortBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelResortBooking.Infrastructure.Persistence.Configurations;

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings", table =>
        {
            table.HasCheckConstraint("CK_Bookings_DateRange", "\"CheckOutDate\" > \"CheckInDate\"");
            table.HasCheckConstraint("CK_Bookings_Adults", "\"Adults\" > 0");
            table.HasCheckConstraint("CK_Bookings_Children", "\"Children\" >= 0");
            table.HasCheckConstraint("CK_Bookings_RoomsCount", "\"RoomsCount\" > 0");
            table.HasCheckConstraint("CK_Bookings_SubTotal", "\"SubTotal\" >= 0");
            table.HasCheckConstraint("CK_Bookings_UpgradeTotal", "\"UpgradeTotal\" >= 0");
            table.HasCheckConstraint("CK_Bookings_DiscountAmount", "\"DiscountAmount\" >= 0");
            table.HasCheckConstraint("CK_Bookings_PointsRedeemed", "\"PointsRedeemed\" >= 0");
            table.HasCheckConstraint("CK_Bookings_TaxAmount", "\"TaxAmount\" >= 0");
            table.HasCheckConstraint("CK_Bookings_TotalAmount", "\"TotalAmount\" >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.BookingNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.CheckInDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.CheckOutDate).HasColumnType("date").IsRequired();
        builder.Property(x => x.Adults).IsRequired();
        builder.Property(x => x.Children).IsRequired();
        builder.Property(x => x.RoomsCount).IsRequired();
        builder.Property(x => x.Status).HasEnumStringConversion().IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.SubTotal).HasMoneyPrecision().IsRequired();
        builder.Property(x => x.UpgradeTotal).HasMoneyPrecision().IsRequired();
        builder.Property(x => x.DiscountAmount).HasMoneyPrecision().IsRequired();
        builder.Property(x => x.PointsRedeemed).IsRequired();
        builder.Property(x => x.TaxAmount).HasMoneyPrecision().IsRequired();
        builder.Property(x => x.TotalAmount).HasMoneyPrecision().IsRequired();
        builder.Property(x => x.CancellationDeadline).HasColumnType("timestamptz");
        builder.Property(x => x.ConfirmedAt).HasColumnType("timestamptz");
        builder.Property(x => x.CancelledAt).HasColumnType("timestamptz");
        builder.Property(x => x.SpecialRequests).HasMaxLength(500);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz").IsRequired();

        builder.HasIndex(x => x.BookingNumber).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.PropertyId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CheckInDate);
        builder.HasIndex(x => x.CheckOutDate);
        builder.HasIndex(x => new { x.PropertyId, x.CheckInDate, x.CheckOutDate });
        builder.HasIndex(x => new { x.UserId, x.Status });

        builder.HasMany(x => x.Rooms)
            .WithOne(x => x.Booking)
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Guests)
            .WithOne(x => x.Booking)
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Upgrades)
            .WithOne(x => x.Booking)
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.StatusHistory)
            .WithOne(x => x.Booking)
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Payments)
            .WithOne(x => x.Booking)
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.LoyaltyTransactions)
            .WithOne(x => x.Booking)
            .HasForeignKey(x => x.BookingId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Review)
            .WithOne(x => x.Booking)
            .HasForeignKey<Review>(x => x.BookingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
