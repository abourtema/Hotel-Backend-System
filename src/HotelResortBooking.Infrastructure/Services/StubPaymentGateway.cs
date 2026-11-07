using HotelResortBooking.Application.Abstractions.Services;

namespace HotelResortBooking.Infrastructure.Services;

public sealed class StubPaymentGateway : IPaymentGateway
{
    public Task<PaymentGatewayChargeResult> ChargeAsync(
        PaymentGatewayChargeRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var failForced = !string.IsNullOrEmpty(request.IdempotencyKey)
            && request.IdempotencyKey.Contains("FAIL", StringComparison.OrdinalIgnoreCase);

        if (request.Amount <= 0 || failForced)
        {
            return Task.FromResult(new PaymentGatewayChargeResult
            {
                Succeeded = false,
                Provider = "Stub",
                FailureReason = request.Amount <= 0
                    ? "Amount must be greater than zero."
                    : "Payment intentionally failed (FAIL idempotency key)."
            });
        }

        return Task.FromResult(new PaymentGatewayChargeResult
        {
            Succeeded = true,
            Provider = "Stub",
            ProviderTransactionId = string.IsNullOrWhiteSpace(request.IdempotencyKey)
                ? Guid.NewGuid().ToString("N")
                : request.IdempotencyKey,
            CardBrand = "Visa",
            CardLast4 = "4242"
        });
    }

    public Task<PaymentGatewayRefundResult> RefundAsync(
        PaymentGatewayRefundRequest request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new PaymentGatewayRefundResult
        {
            Succeeded = request.Amount > 0,
            ProviderRefundId = Guid.NewGuid().ToString("N"),
            FailureReason = request.Amount > 0 ? null : "Amount must be greater than zero."
        });
    }
}
