using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.UseCases.Payments;

public sealed class CreatePaymentResponse
{
    public Guid PaymentId { get; init; }

    public Guid BookingId { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; } = string.Empty;

    public PaymentStatus Status { get; init; }

    public PaymentMethodType PaymentMethod { get; init; }
}
