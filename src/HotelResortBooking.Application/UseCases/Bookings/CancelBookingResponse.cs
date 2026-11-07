namespace HotelResortBooking.Application.UseCases.Bookings;

public sealed class CancelBookingResponse
{
    public BookingDto Booking { get; init; } = null!;
}
