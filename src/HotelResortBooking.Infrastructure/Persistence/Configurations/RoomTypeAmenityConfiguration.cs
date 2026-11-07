using HotelResortBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelResortBooking.Infrastructure.Persistence.Configurations;

public sealed class RoomTypeAmenityConfiguration : IEntityTypeConfiguration<RoomTypeAmenity>
{
    public void Configure(EntityTypeBuilder<RoomTypeAmenity> builder)
    {
        builder.ToTable("RoomTypeAmenities");

        builder.HasKey(x => new { x.RoomTypeId, x.AmenityId });

        builder.HasOne(x => x.Amenity)
            .WithMany(x => x.RoomTypeAmenities)
            .HasForeignKey(x => x.AmenityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.AmenityId);
    }
}
