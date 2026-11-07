using HotelResortBooking.Api.Contracts.Payments;
using HotelResortBooking.Application.UseCases.Payments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelResortBooking.Api.Controllers;

[Authorize]
[Route("api/v1/payments")]
public sealed class PaymentsController : ApiControllerBase
{
    private readonly ICreatePaymentUseCase _createPayment;
    private readonly IProcessPaymentUseCase _processPayment;
    private readonly IGetPaymentUseCase _getPayment;
    private readonly IHandlePaymentFailureUseCase _handleFailure;
    private readonly IPayBookingWithCardUseCase _payWithCard;

    public PaymentsController(
        ICreatePaymentUseCase createPayment,
        IProcessPaymentUseCase processPayment,
        IGetPaymentUseCase getPayment,
        IHandlePaymentFailureUseCase handleFailure,
        IPayBookingWithCardUseCase payWithCard)
    {
        _createPayment = createPayment;
        _processPayment = processPayment;
        _getPayment = getPayment;
        _handleFailure = handleFailure;
        _payWithCard = payWithCard;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreatePaymentResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create(
        [FromBody] CreatePaymentApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _createPayment.ExecuteAsync(
            request.ToUseCaseRequest(CanManageGuestPayments()),
            cancellationToken);
        return HandleResult(result, created => CreatedAtAction(
            nameof(GetById),
            new { paymentId = created.PaymentId },
            created));
    }

    [HttpPost("card")]
    [ProducesResponseType(typeof(PayBookingWithCardResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> PayWithCard(
        [FromBody] CardPaymentApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _payWithCard.ExecuteAsync(
            request.ToUseCaseRequest(CanManageGuestPayments()),
            cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("{paymentId:guid}")]
    [ProducesResponseType(typeof(GetPaymentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid paymentId,
        CancellationToken cancellationToken)
    {
        var result = await _getPayment.ExecuteAsync(
            new GetPaymentRequest
            {
                PaymentId = paymentId,
                BypassOwnershipCheck = CanManageGuestPayments()
            },
            cancellationToken);

        return HandleResult(result);
    }

    [HttpPost("{paymentId:guid}/confirm")]
    [ProducesResponseType(typeof(ProcessPaymentResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Confirm(
        [FromRoute] Guid paymentId,
        [FromBody] ConfirmPaymentApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _processPayment.ExecuteAsync(
            request.ToUseCaseRequest(paymentId, CanManageGuestPayments()),
            cancellationToken);

        return HandleResult(result);
    }

    [HttpPost("{paymentId:guid}/fail")]
    [ProducesResponseType(typeof(HandlePaymentFailureResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Fail(
        [FromRoute] Guid paymentId,
        [FromBody] HandlePaymentFailureRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _handleFailure.ExecuteAsync(
            new HandlePaymentFailureRequest
            {
                PaymentId = paymentId,
                FailureReason = request.FailureReason,
                BypassOwnershipCheck = CanManageGuestPayments()
            },
            cancellationToken);

        return HandleResult(result);
    }
}
