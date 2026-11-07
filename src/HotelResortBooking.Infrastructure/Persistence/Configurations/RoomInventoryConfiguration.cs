using HotelResortBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelResortBooking.Infrastructure.Persistence.Configurations;

public sealed class RoomInventoryConfiguration : IEntityTypeConfiguration<RoomInventory>
{
    public void Configure(EntityTypeBuilder<RoomInventory> builder)
    {
        builder.ToTable("RoomInventory", table =>
        {
            table.HasCheckConstraint("CK_RoomInventory_TotalRooms", "\"TotalRooms\" >= 0");
            table.HasCheckConstraint("CK_RoomInventory_AvailableRooms", "\"AvailableRooms\" >= 0");
            table.HasCheckConstraint(
                "CK_RoomInventory_AvailableNotExceedTotal",
                "\"AvailableRooms\" <= \"TotalRooms\"");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Date).HasColumnType("date").IsRequired();
        builder.Property(x => x.TotalRooms).IsRequired();
        builder.Property(x => x.AvailableRooms).IsRequired();

        builder.ConfigureAuditableEntity();

        builder.HasIndex(x => new { x.RoomTypeId, x.Date }).IsUnique();
        builder.HasIndex(x => x.Date);
        builder.HasIndex(x => new { x.Date, x.AvailableRooms });
    }
}
