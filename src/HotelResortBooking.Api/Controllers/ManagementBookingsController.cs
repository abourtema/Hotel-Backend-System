using HotelResortBooking.Api.Contracts.Bookings;
using HotelResortBooking.Application.Features.Management.Bookings;
using HotelResortBooking.Application.Security;
using HotelResortBooking.Application.UseCases.Bookings;
using HotelResortBooking.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelResortBooking.Api.Controllers;

[Authorize]
[Route("api/v1/management/bookings")]
public sealed class ManagementBookingsController : ApiControllerBase
{
    private readonly IBookingManagementCommands _bookings;

    public ManagementBookingsController(IBookingManagementCommands bookings) => _bookings = bookings;

    [HttpGet]
    [Authorize(Policy = Permissions.BookingsView)]
    public async Task<IActionResult> List(
        [FromQuery] string? bookingNumber,
        [FromQuery] Guid? userId,
        [FromQuery] Guid? propertyId,
        [FromQuery] BookingStatus? status,
        [FromQuery] DateOnly? checkInDate,
        [FromQuery] DateOnly? checkOutDate,
        [FromQuery] PaymentStatus? paymentStatus,
        [FromQuery] PaymentMethodType? paymentMethod,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => HandleResult(await _bookings.ListAsync(new GetManagedBookingsQuery
        {
            BookingNumber = bookingNumber,
            UserId = userId,
            PropertyId = propertyId,
            Status = status,
            CheckInDate = checkInDate,
            CheckOutDate = checkOutDate,
            PaymentStatus = paymentStatus,
            PaymentMethod = paymentMethod,
            Page = page,
            PageSize = pageSize
        }, cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.BookingsView)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => HandleResult(await _bookings.GetAsync(id, cancellationToken));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.BookingsUpdate)]
    public async Task<IActionResult> Modify(Guid id, [FromBody] ModifyBookingRequest request, CancellationToken cancellationToken)
        => HandleResult(await _bookings.ModifyAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/cancel")]
    [Authorize(Policy = Permissions.BookingsCancel)]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelBookingRequest? request, CancellationToken cancellationToken)
        => HandleResult(await _bookings.CancelAsync(id, request?.Reason, cancellationToken));

    [HttpPost("{id:guid}/confirm")]
    [Authorize(Policy = Permissions.BookingsUpdate)]
    [ProducesResponseType(typeof(ConfirmBookingResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Confirm(
        Guid id,
        [FromBody] ConfirmBookingApiRequest? request,
        CancellationToken cancellationToken)
        => HandleResult(await _bookings.ConfirmAsync(id, request?.Reason, cancellationToken));

    [HttpPost("{id:guid}/complete")]
    [Authorize(Policy = Permissions.BookingsUpdate)]
    [ProducesResponseType(typeof(CompleteBookingResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Complete(
        Guid id,
        [FromBody] CompleteBookingApiRequest? request,
        CancellationToken cancellationToken)
        => HandleResult(await _bookings.CompleteAsync(id, request?.Reason, cancellationToken));
}
