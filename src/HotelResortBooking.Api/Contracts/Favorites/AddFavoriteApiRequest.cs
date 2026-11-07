using HotelResortBooking.Application.UseCases.Wishlist;

namespace HotelResortBooking.Api.Contracts.Favorites;

public sealed class AddFavoriteApiRequest
{
    public Guid HotelId { get; init; }

    public AddToWishlistRequest ToUseCaseRequest() => new()
    {
        PropertyId = HotelId
    };
}
