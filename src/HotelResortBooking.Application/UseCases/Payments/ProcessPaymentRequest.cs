namespace HotelResortBooking.Application.UseCases.Payments;

public sealed class ProcessPaymentRequest
{
    public Guid PaymentId { get; init; }

    public string? PaymentMethodToken { get; init; }

    public string? SavedProviderPaymentMethodId { get; init; }

    public string IdempotencyKey { get; init; } = string.Empty;

    public bool BypassOwnershipCheck { get; init; }
}
