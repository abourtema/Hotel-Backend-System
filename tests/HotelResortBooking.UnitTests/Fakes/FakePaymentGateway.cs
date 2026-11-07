using HotelResortBooking.Application.Abstractions.Services;

namespace HotelResortBooking.UnitTests.Fakes;

public sealed class FakePaymentGateway : IPaymentGateway
{
    public Task<PaymentGatewayChargeResult> ChargeAsync(
        PaymentGatewayChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0
            || request.IdempotencyKey.Contains("FAIL", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new PaymentGatewayChargeResult
            {
                Succeeded = false,
                Provider = "fake",
                ProviderTransactionId = request.IdempotencyKey,
                FailureReason = "Payment declined"
            });
        }

        return Task.FromResult(new PaymentGatewayChargeResult
        {
            Succeeded = true,
            Provider = "fake",
            ProviderTransactionId = request.IdempotencyKey,
            CardBrand = "Visa",
            CardLast4 = "4242"
        });
    }

    public Task<PaymentGatewayRefundResult> RefundAsync(
        PaymentGatewayRefundRequest request,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new PaymentGatewayRefundResult
        {
            Succeeded = request.Amount > 0,
            ProviderRefundId = "refund-1"
        });
}
