using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Wishlist;

public interface IAddToWishlistUseCase
{
    Task<Result<AddToWishlistResponse>> ExecuteAsync(
        AddToWishlistRequest request,
        CancellationToken cancellationToken = default);
}
