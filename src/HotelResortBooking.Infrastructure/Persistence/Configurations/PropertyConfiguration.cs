using HotelResortBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelResortBooking.Infrastructure.Persistence.Configurations;

public sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("Properties", table =>
        {
            table.HasCheckConstraint("CK_Properties_StarRating", "\"StarRating\" >= 0 AND \"StarRating\" <= 5");
            table.HasCheckConstraint("CK_Properties_ReviewScore", "\"ReviewScore\" >= 0 AND \"ReviewScore\" <= 5");
            table.HasCheckConstraint("CK_Properties_ReviewCount", "\"ReviewCount\" >= 0");
            table.HasCheckConstraint("CK_Properties_Latitude", "\"Latitude\" >= -90 AND \"Latitude\" <= 90");
            table.HasCheckConstraint("CK_Properties_Longitude", "\"Longitude\" >= -180 AND \"Longitude\" <= 180");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasColumnType("text").IsRequired();
        builder.Property(x => x.PropertyType).HasEnumStringConversion().IsRequired();
        builder.Property(x => x.StarRating).IsRequired();
        builder.Property(x => x.ReviewScore).HasPrecision(3, 2).IsRequired();
        builder.Property(x => x.ReviewCount).IsRequired();
        builder.Property(x => x.AddressLine).HasMaxLength(300).IsRequired();
        builder.Property(x => x.City).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Country).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PostalCode).HasMaxLength(20);
        builder.Property(x => x.Latitude).IsRequired();
        builder.Property(x => x.Longitude).IsRequired();
        builder.Property(x => x.CheckInTime).HasColumnType("time").IsRequired();
        builder.Property(x => x.CheckOutTime).HasColumnType("time").IsRequired();
        builder.Property(x => x.CancellationPolicy).HasColumnType("text");
        builder.Property(x => x.DepositPolicy).HasColumnType("text");
        builder.Property(x => x.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.UpdatedAt).HasColumnType("timestamptz").IsRequired();

        builder.HasIndex(x => x.City);
        builder.HasIndex(x => x.Country);
        builder.HasIndex(x => x.PropertyType);
        builder.HasIndex(x => x.StarRating);
        builder.HasIndex(x => x.Latitude);
        builder.HasIndex(x => x.Longitude);
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => new { x.City, x.Country, x.IsActive });
        builder.HasIndex(x => new { x.Latitude, x.Longitude });

        builder.HasMany(x => x.Images)
            .WithOne(x => x.Property)
            .HasForeignKey(x => x.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.PropertyAmenities)
            .WithOne(x => x.Property)
            .HasForeignKey(x => x.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.RoomTypes)
            .WithOne(x => x.Property)
            .HasForeignKey(x => x.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.UpgradeOptions)
            .WithOne(x => x.Property)
            .HasForeignKey(x => x.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Bookings)
            .WithOne(x => x.Property)
            .HasForeignKey(x => x.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Reviews)
            .WithOne(x => x.Property)
            .HasForeignKey(x => x.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.WishlistItems)
            .WithOne(x => x.Property)
            .HasForeignKey(x => x.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
