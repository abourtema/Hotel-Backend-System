namespace HotelResortBooking.Domain.Entities;

using HotelResortBooking.Domain.Common;

public sealed class Wishlist : AuditableEntity
{
    public Guid UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public User User { get; set; } = null!;

    public ICollection<WishlistItem> Items { get; set; } = new List<WishlistItem>();
}
