using HotelResortBooking.Application.Features.Management.Payments;
using HotelResortBooking.Application.Security;
using HotelResortBooking.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelResortBooking.Api.Controllers;

[Authorize]
[Route("api/v1/management/payments")]
public sealed class ManagementPaymentsController : ApiControllerBase
{
    private readonly IPaymentManagementCommands _payments;

    public ManagementPaymentsController(IPaymentManagementCommands payments) => _payments = payments;

    [HttpGet]
    [Authorize(Policy = Permissions.PaymentsView)]
    public async Task<IActionResult> List(
        [FromQuery] Guid? bookingId,
        [FromQuery] Guid? customerId,
        [FromQuery] PaymentStatus? status,
        [FromQuery] PaymentMethodType? paymentMethod,
        [FromQuery] DateTimeOffset? dateFrom,
        [FromQuery] DateTimeOffset? dateTo,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => HandleResult(await _payments.ListAsync(new GetManagedPaymentsQuery
        {
            BookingId = bookingId,
            CustomerId = customerId,
            Status = status,
            PaymentMethod = paymentMethod,
            DateFrom = dateFrom,
            DateTo = dateTo,
            Page = page,
            PageSize = pageSize
        }, cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.PaymentsView)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => HandleResult(await _payments.GetAsync(id, cancellationToken));

    [HttpPost("{id:guid}/confirm")]
    [Authorize(Policy = Permissions.PaymentsRefund)]
    [ProducesResponseType(typeof(ManagedPaymentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Confirm(Guid id, CancellationToken cancellationToken)
        => HandleResult(await _payments.ConfirmAsync(id, cancellationToken));

    [HttpPost("{id:guid}/refund")]
    [Authorize(Policy = Permissions.PaymentsRefund)]
    public async Task<IActionResult> Refund(Guid id, CancellationToken cancellationToken)
        => HandleResult(await _payments.RefundAsync(id, cancellationToken));
}
