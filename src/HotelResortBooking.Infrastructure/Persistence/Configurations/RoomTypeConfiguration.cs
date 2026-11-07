using HotelResortBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelResortBooking.Infrastructure.Persistence.Configurations;

public sealed class RoomTypeConfiguration : IEntityTypeConfiguration<RoomType>
{
    public void Configure(EntityTypeBuilder<RoomType> builder)
    {
        builder.ToTable("RoomTypes", table =>
        {
            table.HasCheckConstraint("CK_RoomTypes_MaxAdults", "\"MaxAdults\" >= 0");
            table.HasCheckConstraint("CK_RoomTypes_MaxChildren", "\"MaxChildren\" >= 0");
            table.HasCheckConstraint("CK_RoomTypes_MaxGuests", "\"MaxGuests\" > 0");
            table.HasCheckConstraint("CK_RoomTypes_BasePricePerNight", "\"BasePricePerNight\" >= 0");
            table.HasCheckConstraint("CK_RoomTypes_RoomSize", "\"RoomSize\" IS NULL OR \"RoomSize\" > 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasColumnType("text").IsRequired();
        builder.Property(x => x.RoomSize).HasPrecision(10, 2);
        builder.Property(x => x.MaxAdults).IsRequired();
        builder.Property(x => x.MaxChildren).IsRequired();
        builder.Property(x => x.MaxGuests).IsRequired();
        builder.Property(x => x.BedType).HasMaxLength(100);
        builder.Property(x => x.BasePricePerNight).HasMoneyPrecision().IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);

        builder.ConfigureAuditableEntity();

        builder.HasIndex(x => x.PropertyId);
        builder.HasIndex(x => new { x.PropertyId, x.IsActive });
        builder.HasIndex(x => x.BasePricePerNight);

        builder.HasMany(x => x.Images)
            .WithOne(x => x.RoomType)
            .HasForeignKey(x => x.RoomTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.RoomTypeAmenities)
            .WithOne(x => x.RoomType)
            .HasForeignKey(x => x.RoomTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Inventories)
            .WithOne(x => x.RoomType)
            .HasForeignKey(x => x.RoomTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.RoomTypeUpgradeOptions)
            .WithOne(x => x.RoomType)
            .HasForeignKey(x => x.RoomTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.BookingRooms)
            .WithOne(x => x.RoomType)
            .HasForeignKey(x => x.RoomTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
