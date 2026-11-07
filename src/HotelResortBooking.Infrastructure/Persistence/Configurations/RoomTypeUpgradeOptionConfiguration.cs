using HotelResortBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelResortBooking.Infrastructure.Persistence.Configurations;

public sealed class RoomTypeUpgradeOptionConfiguration : IEntityTypeConfiguration<RoomTypeUpgradeOption>
{
    public void Configure(EntityTypeBuilder<RoomTypeUpgradeOption> builder)
    {
        builder.ToTable("RoomTypeUpgradeOptions");

        builder.HasKey(x => new { x.RoomTypeId, x.UpgradeOptionId });

        builder.HasIndex(x => x.UpgradeOptionId);
    }
}
