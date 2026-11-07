using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.UseCases.Payments;

public sealed class ProcessPaymentResponse
{
    public Guid PaymentId { get; init; }

    public Guid BookingId { get; init; }

    public PaymentStatus Status { get; init; }

    public string? ProviderTransactionId { get; init; }

    public string? FailureReason { get; init; }

    public BookingStatus? BookingStatus { get; init; }
}
