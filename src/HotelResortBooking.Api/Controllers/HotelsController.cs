using HotelResortBooking.Api.Contracts.Hotels;
using HotelResortBooking.Api.Contracts.Map;
using HotelResortBooking.Api.Contracts.Rooms;
using HotelResortBooking.Application.UseCases.Map;
using HotelResortBooking.Application.UseCases.Properties;
using HotelResortBooking.Application.UseCases.Rooms;
using HotelResortBooking.Application.UseCases.Search;
using HotelResortBooking.Application.UseCases.Upgrades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelResortBooking.Api.Controllers;

[AllowAnonymous]
[Route("api/v1/hotels")]
public sealed class HotelsController : ApiControllerBase
{
    private readonly ISearchPropertiesUseCase _searchProperties;
    private readonly IGetPropertyDetailsUseCase _getPropertyDetails;
    private readonly IGetAvailableRoomsUseCase _getAvailableRooms;
    private readonly ISearchPropertiesByLocationUseCase _searchByLocation;
    private readonly IGetAvailableUpgradesUseCase _getAvailableUpgrades;

    public HotelsController(
        ISearchPropertiesUseCase searchProperties,
        IGetPropertyDetailsUseCase getPropertyDetails,
        IGetAvailableRoomsUseCase getAvailableRooms,
        ISearchPropertiesByLocationUseCase searchByLocation,
        IGetAvailableUpgradesUseCase getAvailableUpgrades)
    {
        _searchProperties = searchProperties;
        _getPropertyDetails = getPropertyDetails;
        _getAvailableRooms = getAvailableRooms;
        _searchByLocation = searchByLocation;
        _getAvailableUpgrades = getAvailableUpgrades;
    }

    [HttpGet("search")]
    [ProducesResponseType(typeof(SearchPropertiesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] HotelSearchQueryParameters parameters,
        CancellationToken cancellationToken)
    {
        var result = await _searchProperties.ExecuteAsync(
            parameters.ToUseCaseRequest(),
            cancellationToken);

        return HandleResult(result);
    }

    [HttpGet("map")]
    [ProducesResponseType(typeof(SearchPropertiesByLocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchByMap(
        [FromQuery] SearchMapQueryParams parameters,
        CancellationToken cancellationToken)
    {
        var result = await _searchByLocation.ExecuteAsync(
            parameters.ToUseCaseRequest(),
            cancellationToken);

        return HandleResult(result);
    }

    [HttpGet("{hotelId:guid}")]
    [ProducesResponseType(typeof(GetPropertyDetailsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDetails(
        [FromRoute] Guid hotelId,
        CancellationToken cancellationToken)
    {
        var result = await _getPropertyDetails.ExecuteAsync(
            new GetPropertyDetailsRequest { PropertyId = hotelId },
            cancellationToken);

        return HandleResult(result);
    }

    [HttpGet("{hotelId:guid}/rooms")]
    [ProducesResponseType(typeof(GetAvailableRoomsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRooms(
        [FromRoute] Guid hotelId,
        [FromQuery] GetAvailableRoomsQueryParams parameters,
        CancellationToken cancellationToken)
    {
        var result = await _getAvailableRooms.ExecuteAsync(
            parameters.ToUseCaseRequest(hotelId),
            cancellationToken);

        return HandleResult(result);
    }

    [HttpGet("{hotelId:guid}/upgrades")]
    [ProducesResponseType(typeof(GetAvailableUpgradesResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUpgrades(
        [FromRoute] Guid hotelId,
        [FromQuery] Guid? roomTypeId,
        CancellationToken cancellationToken)
    {
        var result = await _getAvailableUpgrades.ExecuteAsync(
            new GetAvailableUpgradesRequest
            {
                PropertyId = hotelId,
                RoomTypeId = roomTypeId
            },
            cancellationToken);

        return HandleResult(result);
    }
}
