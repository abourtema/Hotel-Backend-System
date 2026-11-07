using HotelResortBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelResortBooking.Infrastructure.Persistence.Configurations;

public sealed class BookingStatusHistoryConfiguration : IEntityTypeConfiguration<BookingStatusHistory>
{
    public void Configure(EntityTypeBuilder<BookingStatusHistory> builder)
    {
        builder.ToTable("BookingStatusHistory");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.OldStatus).HasEnumStringConversion();
        builder.Property(x => x.NewStatus).HasEnumStringConversion().IsRequired();
        builder.Property(x => x.ChangedAt).HasColumnType("timestamptz").IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500);

        builder.HasIndex(x => x.BookingId);
        builder.HasIndex(x => new { x.BookingId, x.ChangedAt });
    }
}
