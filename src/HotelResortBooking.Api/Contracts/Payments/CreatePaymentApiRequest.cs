using HotelResortBooking.Application.UseCases.Payments;
using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Api.Contracts.Payments;

public sealed class CreatePaymentApiRequest
{
    public Guid BookingId { get; init; }

    public PaymentMethodType PaymentMethod { get; init; } = PaymentMethodType.CreditCard;

    public string? IdempotencyKey { get; init; }

    public string? PaymentMethodToken { get; init; }

    public string? CardBrand { get; init; }

    public string? CardLast4 { get; init; }

    public CreatePaymentRequest ToUseCaseRequest(bool bypassOwnershipCheck = false) => new()
    {
        BookingId = BookingId,
        PaymentMethod = PaymentMethod,
        IdempotencyKey = IdempotencyKey,
        PaymentMethodToken = PaymentMethodToken,
        CardBrand = CardBrand,
        CardLast4 = CardLast4,
        BypassOwnershipCheck = bypassOwnershipCheck
    };
}
