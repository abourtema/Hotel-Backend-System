using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Wishlist;

public sealed class GetWishlistUseCase : IGetWishlistUseCase
{
    private readonly IAccessContext _accessContext;
    private readonly IWishlistRepository _wishlistRepository;

    public GetWishlistUseCase(IAccessContext accessContext, IWishlistRepository wishlistRepository)
    {
        _accessContext = accessContext;
        _wishlistRepository = wishlistRepository;
    }

    public async Task<Result<GetWishlistResponse>> ExecuteAsync(
        GetWishlistRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_accessContext.IsAuthenticated || _accessContext.UserId is null)
        {
            return Result.Failure<GetWishlistResponse>(BusinessErrors.AuthenticationRequired());
        }

        var wishlist = await _wishlistRepository.GetOrCreateDefaultAsync(
            _accessContext.UserId.Value,
            cancellationToken);

        return Result.Success(new GetWishlistResponse
        {
            WishlistId = wishlist.Id,
            Name = wishlist.Name,
            Items = wishlist.Items
        });
    }
}
