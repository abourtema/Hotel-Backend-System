using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Wishlist;

public interface IRemoveFromWishlistUseCase
{
    Task<Result<RemoveFromWishlistResponse>> ExecuteAsync(
        RemoveFromWishlistRequest request,
        CancellationToken cancellationToken = default);
}
