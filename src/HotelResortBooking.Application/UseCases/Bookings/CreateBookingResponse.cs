using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.UseCases.Bookings;

public sealed class CreateBookingResponse
{
    public BookingDto Booking { get; init; } = null!;

    public Guid? PaymentId { get; init; }

    public PaymentStatus? PaymentStatus { get; init; }

    public PaymentMethodType? PaymentMethod { get; init; }
}
