using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.UseCases.Payments;

public sealed class PayBookingWithCardResponse
{
    public Guid PaymentId { get; init; }

    public Guid BookingId { get; init; }

    public PaymentStatus PaymentStatus { get; init; }

    public BookingStatus BookingStatus { get; init; }

    public string? ProviderTransactionId { get; init; }

    public string? FailureReason { get; init; }

    public string? CardBrand { get; init; }

    public string? CardLast4 { get; init; }
}
