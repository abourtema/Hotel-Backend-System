using HotelResortBooking.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HotelResortBooking.Infrastructure.Persistence.Configurations;

public sealed class WishlistItemConfiguration : IEntityTypeConfiguration<WishlistItem>
{
    public void Configure(EntityTypeBuilder<WishlistItem> builder)
    {
        builder.ToTable("WishlistItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CreatedAt).ConfigureCreatedAt();

        builder.HasIndex(x => new { x.WishlistId, x.PropertyId }).IsUnique();
        builder.HasIndex(x => x.PropertyId);
    }
}
