using HotelResortBooking.Application.Abstractions.Repositories;

namespace HotelResortBooking.Application.UseCases.Wishlist;

public sealed class AddToWishlistResponse
{
    public WishlistItemData Item { get; init; } = null!;
}
