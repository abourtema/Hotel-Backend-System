using HotelResortBooking.Api.Contracts.Bookings;
using HotelResortBooking.Application.UseCases.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelResortBooking.Api.Controllers;

[Authorize]
[Route("api/v1/checkout")]
public sealed class BookingCheckoutController : ApiControllerBase
{
    private readonly ICreateBookingDraftUseCase _createDraft;
    private readonly IGetBookingSummaryUseCase _getSummary;

    public BookingCheckoutController(
        ICreateBookingDraftUseCase createDraft,
        IGetBookingSummaryUseCase getSummary)
    {
        _createDraft = createDraft;
        _getSummary = getSummary;
    }

    [HttpPost("draft")]
    [ProducesResponseType(typeof(CreateBookingDraftResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> CreateDraft(
        [FromBody] CreateBookingApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _createDraft.ExecuteAsync(request.ToDraftRequest(), cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("summary")]
    [ProducesResponseType(typeof(GetBookingSummaryResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(
        [FromBody] CreateBookingApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _getSummary.ExecuteAsync(request.ToSummaryRequest(), cancellationToken);
        return HandleResult(result);
    }
}
