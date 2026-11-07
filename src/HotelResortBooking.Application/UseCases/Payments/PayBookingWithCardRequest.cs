namespace HotelResortBooking.Application.UseCases.Payments;

/// <summary>
/// Pays a booking with a card token. Never accepts or stores CVV.
/// </summary>
public sealed class PayBookingWithCardRequest
{
    public Guid BookingId { get; init; }

    /// <summary>
    /// Tokenized payment method from the gateway. CVV must never be sent here.
    /// </summary>
    public string PaymentMethodToken { get; init; } = string.Empty;

    public string IdempotencyKey { get; init; } = string.Empty;

    public string? CardBrand { get; init; }

    public string? CardLast4 { get; init; }

    public bool BypassOwnershipCheck { get; init; }
}
