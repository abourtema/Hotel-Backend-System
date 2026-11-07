using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.UseCases.Payments;

public sealed class HandlePaymentFailureResponse
{
    public Guid PaymentId { get; init; }

    public Guid BookingId { get; init; }

    public PaymentStatus Status { get; init; }

    public string? FailureReason { get; init; }
}
