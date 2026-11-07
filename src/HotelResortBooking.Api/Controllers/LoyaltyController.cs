using HotelResortBooking.Api.Contracts.Loyalty;
using HotelResortBooking.Application.UseCases.Loyalty;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelResortBooking.Api.Controllers;

[Authorize]
[Route("api/v1/loyalty")]
public sealed class LoyaltyController : ApiControllerBase
{
    private readonly IGetLoyaltyBalanceUseCase _getBalance;
    private readonly IGetLoyaltyTransactionsUseCase _getTransactions;
    private readonly ICalculatePointsRedemptionUseCase _calculate;
    private readonly IRedeemPointsUseCase _redeem;
    private readonly IRefundPointsUseCase _refund;
    private readonly IApplyLoyaltyPointsToBookingUseCase _applyToBooking;

    public LoyaltyController(
        IGetLoyaltyBalanceUseCase getBalance,
        IGetLoyaltyTransactionsUseCase getTransactions,
        ICalculatePointsRedemptionUseCase calculate,
        IRedeemPointsUseCase redeem,
        IRefundPointsUseCase refund,
        IApplyLoyaltyPointsToBookingUseCase applyToBooking)
    {
        _getBalance = getBalance;
        _getTransactions = getTransactions;
        _calculate = calculate;
        _redeem = redeem;
        _refund = refund;
        _applyToBooking = applyToBooking;
    }

    [HttpGet("balance")]
    [ProducesResponseType(typeof(GetLoyaltyBalanceResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBalance(CancellationToken cancellationToken)
    {
        return HandleResult(await _getBalance.ExecuteAsync(cancellationToken));
    }

    [HttpGet("transactions")]
    [ProducesResponseType(typeof(GetLoyaltyTransactionsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTransactions(CancellationToken cancellationToken)
    {
        return HandleResult(await _getTransactions.ExecuteAsync(cancellationToken));
    }

    [HttpPost("calculate")]
    [ProducesResponseType(typeof(CalculatePointsRedemptionResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Calculate(
        [FromBody] CalculateRedemptionApiRequest request,
        CancellationToken cancellationToken)
    {
        return HandleResult(await _calculate.ExecuteAsync(request.ToUseCaseRequest(), cancellationToken));
    }

    [HttpPost("redeem")]
    [ProducesResponseType(typeof(RedeemPointsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Redeem(
        [FromBody] RedeemPointsApiRequest request,
        CancellationToken cancellationToken)
    {
        return HandleResult(await _redeem.ExecuteAsync(request.ToUseCaseRequest(), cancellationToken));
    }

    [HttpPost("refund")]
    [ProducesResponseType(typeof(RefundPointsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refund(
        [FromBody] RedeemPointsApiRequest request,
        CancellationToken cancellationToken)
    {
        return HandleResult(await _refund.ExecuteAsync(
            new RefundPointsRequest
            {
                BookingId = request.BookingId,
                Points = request.Points,
                Description = request.Description
            },
            cancellationToken));
    }

    [HttpPost("apply")]
    [ProducesResponseType(typeof(ApplyLoyaltyPointsToBookingResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApplyToBooking(
        [FromBody] ApplyLoyaltyPointsApiRequest request,
        CancellationToken cancellationToken)
    {
        return HandleResult(await _applyToBooking.ExecuteAsync(request.ToUseCaseRequest(), cancellationToken));
    }
}
