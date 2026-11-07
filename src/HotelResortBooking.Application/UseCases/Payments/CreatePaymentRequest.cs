using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.UseCases.Payments;

public sealed class CreatePaymentRequest
{
    public Guid BookingId { get; init; }

    public PaymentMethodType PaymentMethod { get; init; } = PaymentMethodType.CreditCard;

    public string? IdempotencyKey { get; init; }

    public string? PaymentMethodToken { get; init; }

    public string? CardBrand { get; init; }

    public string? CardLast4 { get; init; }

    public bool BypassOwnershipCheck { get; init; }
}
