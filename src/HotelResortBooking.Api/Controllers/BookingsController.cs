using HotelResortBooking.Api.Contracts.Bookings;
using HotelResortBooking.Api.Contracts.Upgrades;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.UseCases.Bookings;
using HotelResortBooking.Application.UseCases.Upgrades;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelResortBooking.Api.Controllers;

[Authorize]
[Route("api/v1/bookings")]
public sealed class BookingsController : ApiControllerBase
{
    private readonly ICreateBookingUseCase _createBooking;
    private readonly IGetBookingUseCase _getBooking;
    private readonly IGetUserBookingsUseCase _getUserBookings;
    private readonly ICancelBookingUseCase _cancelBooking;
    private readonly IModifyBookingUseCase _modifyBooking;
    private readonly IAddBookingUpgradeUseCase _addUpgrade;
    private readonly IRemoveBookingUpgradeUseCase _removeUpgrade;

    public BookingsController(
        ICreateBookingUseCase createBooking,
        IGetBookingUseCase getBooking,
        IGetUserBookingsUseCase getUserBookings,
        ICancelBookingUseCase cancelBooking,
        IModifyBookingUseCase modifyBooking,
        IAddBookingUpgradeUseCase addUpgrade,
        IRemoveBookingUpgradeUseCase removeUpgrade)
    {
        _createBooking = createBooking;
        _getBooking = getBooking;
        _getUserBookings = getUserBookings;
        _cancelBooking = cancelBooking;
        _modifyBooking = modifyBooking;
        _addUpgrade = addUpgrade;
        _removeUpgrade = removeUpgrade;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateBookingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateBookingApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _createBooking.ExecuteAsync(request.ToUseCaseRequest(), cancellationToken);
        return HandleResult(result, created => CreatedAtAction(
            nameof(GetById),
            new { bookingId = created.Booking.Id },
            created));
    }

    [HttpGet]
    [ProducesResponseType(typeof(GetUserBookingsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMine(
        [FromQuery] BookingListGroup? group,
        CancellationToken cancellationToken)
    {
        var result = await _getUserBookings.ExecuteAsync(
            new GetUserBookingsRequest { Group = group },
            cancellationToken);

        return HandleResult(result);
    }

    [HttpGet("{bookingId:guid}")]
    [ProducesResponseType(typeof(GetBookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid bookingId,
        CancellationToken cancellationToken)
    {
        var result = await _getBooking.ExecuteAsync(
            new GetBookingRequest { BookingId = bookingId },
            cancellationToken);

        return HandleResult(result);
    }

    [HttpPut("{bookingId:guid}")]
    [ProducesResponseType(typeof(ModifyBookingResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Modify(
        [FromRoute] Guid bookingId,
        [FromBody] ModifyBookingApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _modifyBooking.ExecuteAsync(
            request.ToUseCaseRequest(bookingId),
            cancellationToken);

        return HandleResult(result);
    }

    [HttpPost("{bookingId:guid}/cancel")]
    [ProducesResponseType(typeof(CancelBookingResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Cancel(
        [FromRoute] Guid bookingId,
        [FromBody] CancelBookingApiRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _cancelBooking.ExecuteAsync(
            new CancelBookingRequest
            {
                BookingId = bookingId,
                Reason = request?.Reason
            },
            cancellationToken);

        return HandleResult(result);
    }

    [HttpPost("{bookingId:guid}/upgrades")]
    [ProducesResponseType(typeof(AddBookingUpgradeResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> AddUpgrade(
        [FromRoute] Guid bookingId,
        [FromBody] AddUpgradeApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _addUpgrade.ExecuteAsync(
            new AddBookingUpgradeRequest
            {
                BookingId = bookingId,
                UpgradeOptionId = request.UpgradeOptionId,
                Quantity = request.Quantity
            },
            cancellationToken);

        return HandleResult(result);
    }

    [HttpDelete("{bookingId:guid}/upgrades/{upgradeOptionId:guid}")]
    [ProducesResponseType(typeof(RemoveBookingUpgradeResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveUpgrade(
        [FromRoute] Guid bookingId,
        [FromRoute] Guid upgradeOptionId,
        CancellationToken cancellationToken)
    {
        var result = await _removeUpgrade.ExecuteAsync(
            new RemoveBookingUpgradeRequest
            {
                BookingId = bookingId,
                UpgradeOptionId = upgradeOptionId
            },
            cancellationToken);

        return HandleResult(result);
    }
}
