namespace HotelResortBooking.Application.UseCases.Bookings;

public sealed class GetBookingResponse
{
    public BookingDto Booking { get; init; } = null!;
}
