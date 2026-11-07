using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Wishlist;

public sealed class AddToWishlistUseCase : IAddToWishlistUseCase
{
    private readonly IAccessContext _accessContext;
    private readonly IWishlistRepository _wishlistRepository;
    private readonly IPropertyRepository _propertyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddToWishlistUseCase(
        IAccessContext accessContext,
        IWishlistRepository wishlistRepository,
        IPropertyRepository propertyRepository,
        IUnitOfWork unitOfWork)
    {
        _accessContext = accessContext;
        _wishlistRepository = wishlistRepository;
        _propertyRepository = propertyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AddToWishlistResponse>> ExecuteAsync(
        AddToWishlistRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_accessContext.IsAuthenticated || _accessContext.UserId is null)
        {
            return Result.Failure<AddToWishlistResponse>(BusinessErrors.AuthenticationRequired());
        }

        if (!await _propertyRepository.ExistsActiveAsync(request.PropertyId, cancellationToken))
        {
            return Result.Failure<AddToWishlistResponse>(BusinessErrors.PropertyNotFound(request.PropertyId));
        }

        var wishlist = await _wishlistRepository.GetOrCreateDefaultAsync(
            _accessContext.UserId.Value,
            cancellationToken);

        if (await _wishlistRepository.ContainsPropertyAsync(wishlist.Id, request.PropertyId, cancellationToken))
        {
            return Result.Failure<AddToWishlistResponse>(BusinessErrors.WishlistItemExists());
        }

        var item = await _wishlistRepository.AddItemAsync(wishlist.Id, request.PropertyId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new AddToWishlistResponse { Item = item });
    }
}
