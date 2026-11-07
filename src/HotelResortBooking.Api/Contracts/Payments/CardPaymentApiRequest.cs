using HotelResortBooking.Application.UseCases.Payments;

namespace HotelResortBooking.Api.Contracts.Payments;

public sealed class CardPaymentApiRequest
{
    public Guid BookingId { get; init; }

    public string PaymentMethodToken { get; init; } = string.Empty;

    public string IdempotencyKey { get; init; } = string.Empty;

    public string? CardBrand { get; init; }

    public string? CardLast4 { get; init; }

    public PayBookingWithCardRequest ToUseCaseRequest(bool bypassOwnershipCheck = false) => new()
    {
        BookingId = BookingId,
        PaymentMethodToken = PaymentMethodToken,
        IdempotencyKey = IdempotencyKey,
        CardBrand = CardBrand,
        CardLast4 = CardLast4,
        BypassOwnershipCheck = bypassOwnershipCheck
    };
}
