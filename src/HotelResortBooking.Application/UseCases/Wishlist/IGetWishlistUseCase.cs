using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Wishlist;

public interface IGetWishlistUseCase
{
    Task<Result<GetWishlistResponse>> ExecuteAsync(
        GetWishlistRequest request,
        CancellationToken cancellationToken = default);
}
