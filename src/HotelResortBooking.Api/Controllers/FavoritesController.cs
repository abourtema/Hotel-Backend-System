using HotelResortBooking.Api.Contracts.Favorites;
using HotelResortBooking.Application.UseCases.Wishlist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelResortBooking.Api.Controllers;

[Authorize]
[Route("api/v1/favorites")]
public sealed class FavoritesController : ApiControllerBase
{
    private readonly IGetWishlistUseCase _getWishlist;
    private readonly IAddToWishlistUseCase _addToWishlist;
    private readonly IRemoveFromWishlistUseCase _removeFromWishlist;

    public FavoritesController(
        IGetWishlistUseCase getWishlist,
        IAddToWishlistUseCase addToWishlist,
        IRemoveFromWishlistUseCase removeFromWishlist)
    {
        _getWishlist = getWishlist;
        _addToWishlist = addToWishlist;
        _removeFromWishlist = removeFromWishlist;
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetWishlistResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        return HandleResult(await _getWishlist.ExecuteAsync(new GetWishlistRequest(), cancellationToken));
    }

    [HttpPost]
    [ProducesResponseType(typeof(AddToWishlistResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Add(
        [FromBody] AddFavoriteApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _addToWishlist.ExecuteAsync(request.ToUseCaseRequest(), cancellationToken);
        return HandleResult(result, created => StatusCode(StatusCodes.Status201Created, created));
    }

    [HttpDelete("{hotelId:guid}")]
    [ProducesResponseType(typeof(RemoveFromWishlistResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Remove(
        [FromRoute] Guid hotelId,
        CancellationToken cancellationToken)
    {
        var result = await _removeFromWishlist.ExecuteAsync(
            new RemoveFromWishlistRequest { PropertyId = hotelId },
            cancellationToken);

        return HandleResult(result);
    }
}
