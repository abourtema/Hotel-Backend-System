using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Wishlist;

public sealed class RemoveFromWishlistUseCase : IRemoveFromWishlistUseCase
{
    private readonly IAccessContext _accessContext;
    private readonly IWishlistRepository _wishlistRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveFromWishlistUseCase(
        IAccessContext accessContext,
        IWishlistRepository wishlistRepository,
        IUnitOfWork unitOfWork)
    {
        _accessContext = accessContext;
        _wishlistRepository = wishlistRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RemoveFromWishlistResponse>> ExecuteAsync(
        RemoveFromWishlistRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_accessContext.IsAuthenticated || _accessContext.UserId is null)
        {
            return Result.Failure<RemoveFromWishlistResponse>(BusinessErrors.AuthenticationRequired());
        }

        var wishlist = await _wishlistRepository.GetOrCreateDefaultAsync(
            _accessContext.UserId.Value,
            cancellationToken);

        var removed = await _wishlistRepository.RemoveItemAsync(
            wishlist.Id,
            request.PropertyId,
            cancellationToken);

        if (!removed)
        {
            return Result.Failure<RemoveFromWishlistResponse>(BusinessErrors.WishlistItemNotFound());
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new RemoveFromWishlistResponse { Removed = true });
    }
}
