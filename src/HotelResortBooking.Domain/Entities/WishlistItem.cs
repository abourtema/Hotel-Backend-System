namespace HotelResortBooking.Domain.Entities;

using HotelResortBooking.Domain.Common;

public sealed class WishlistItem : Entity
{
    public Guid WishlistId { get; set; }

    public Guid PropertyId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Wishlist Wishlist { get; set; } = null!;

    public Property Property { get; set; } = null!;
}
