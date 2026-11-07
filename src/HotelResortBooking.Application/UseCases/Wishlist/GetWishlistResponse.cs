using HotelResortBooking.Application.Abstractions.Repositories;

namespace HotelResortBooking.Application.UseCases.Wishlist;

public sealed class GetWishlistResponse
{
    public Guid WishlistId { get; init; }

    public string Name { get; init; } = string.Empty;

    public IReadOnlyList<WishlistItemData> Items { get; init; } = Array.Empty<WishlistItemData>();
}
