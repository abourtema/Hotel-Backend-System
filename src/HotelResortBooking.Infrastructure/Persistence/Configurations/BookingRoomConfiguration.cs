using HotelResortBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelResortBooking.Infrastructure.Persistence.Configurations;

public sealed class BookingRoomConfiguration : IEntityTypeConfiguration<BookingRoom>
{
    public void Configure(EntityTypeBuilder<BookingRoom> builder)
    {
        builder.ToTable("BookingRooms", table =>
        {
            table.HasCheckConstraint("CK_BookingRooms_Quantity", "\"Quantity\" > 0");
            table.HasCheckConstraint("CK_BookingRooms_PricePerNight", "\"PricePerNight\" >= 0");
            table.HasCheckConstraint("CK_BookingRooms_Nights", "\"Nights\" > 0");
            table.HasCheckConstraint("CK_BookingRooms_SubTotal", "\"SubTotal\" >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.PricePerNight).HasMoneyPrecision().IsRequired();
        builder.Property(x => x.Nights).IsRequired();
        builder.Property(x => x.SubTotal).HasMoneyPrecision().IsRequired();
        builder.Property(x => x.CreatedAt).ConfigureCreatedAt();

        builder.HasIndex(x => x.BookingId);
        builder.HasIndex(x => x.RoomTypeId);
    }
}
