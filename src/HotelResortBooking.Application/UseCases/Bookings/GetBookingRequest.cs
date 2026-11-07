namespace HotelResortBooking.Application.UseCases.Bookings;

public sealed class GetBookingRequest
{
    public Guid? BookingId { get; init; }

    public string? BookingNumber { get; init; }
}
