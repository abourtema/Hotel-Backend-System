using HotelResortBooking.Application.Abstractions.Repositories;

namespace HotelResortBooking.UnitTests.Fakes;

public sealed class FakeWishlistRepository : IWishlistRepository
{
    public List<WishlistData> Wishlists { get; } = [];
    public Dictionary<Guid, string> PropertyNames { get; } = [];

    public Task<WishlistData> GetOrCreateDefaultAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var existing = Wishlists.FirstOrDefault(w => w.UserId == userId);
        if (existing is not null)
        {
            return Task.FromResult(existing);
        }

        var created = new WishlistData
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "Default",
            Items = []
        };
        Wishlists.Add(created);
        return Task.FromResult(created);
    }

    public Task<bool> ContainsPropertyAsync(Guid wishlistId, Guid propertyId, CancellationToken cancellationToken = default)
    {
        var wishlist = Wishlists.FirstOrDefault(w => w.Id == wishlistId);
        return Task.FromResult(wishlist?.Items.Any(i => i.PropertyId == propertyId) == true);
    }

    public Task<WishlistItemData> AddItemAsync(Guid wishlistId, Guid propertyId, CancellationToken cancellationToken = default)
    {
        var wishlist = Wishlists.First(w => w.Id == wishlistId);
        var item = new WishlistItemData
        {
            Id = Guid.NewGuid(),
            WishlistId = wishlistId,
            PropertyId = propertyId,
            PropertyName = PropertyNames.GetValueOrDefault(propertyId, "Property"),
            City = "City",
            Country = "Country",
            CreatedAt = DateTimeOffset.UtcNow
        };

        var items = wishlist.Items.ToList();
        items.Add(item);
        ReplaceWishlist(wishlist, items);
        return Task.FromResult(item);
    }

    public Task<bool> RemoveItemAsync(Guid wishlistId, Guid propertyId, CancellationToken cancellationToken = default)
    {
        var wishlist = Wishlists.FirstOrDefault(w => w.Id == wishlistId);
        if (wishlist is null)
        {
            return Task.FromResult(false);
        }

        var items = wishlist.Items.ToList();
        var removed = items.RemoveAll(i => i.PropertyId == propertyId) > 0;
        if (removed)
        {
            ReplaceWishlist(wishlist, items);
        }

        return Task.FromResult(removed);
    }

    private void ReplaceWishlist(WishlistData wishlist, List<WishlistItemData> items)
    {
        var index = Wishlists.FindIndex(w => w.Id == wishlist.Id);
        Wishlists[index] = new WishlistData
        {
            Id = wishlist.Id,
            UserId = wishlist.UserId,
            Name = wishlist.Name,
            Items = items
        };
    }
}
