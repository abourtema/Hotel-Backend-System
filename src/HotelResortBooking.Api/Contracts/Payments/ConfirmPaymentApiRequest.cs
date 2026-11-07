using HotelResortBooking.Application.UseCases.Payments;

namespace HotelResortBooking.Api.Contracts.Payments;

public sealed class ConfirmPaymentApiRequest
{
    public string? PaymentMethodToken { get; init; }

    public string? SavedProviderPaymentMethodId { get; init; }

    public string IdempotencyKey { get; init; } = string.Empty;

    public ProcessPaymentRequest ToUseCaseRequest(Guid paymentId, bool bypassOwnershipCheck = false) => new()
    {
        PaymentId = paymentId,
        PaymentMethodToken = PaymentMethodToken,
        SavedProviderPaymentMethodId = SavedProviderPaymentMethodId,
        IdempotencyKey = IdempotencyKey,
        BypassOwnershipCheck = bypassOwnershipCheck
    };
}
