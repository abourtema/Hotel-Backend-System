namespace HotelResortBooking.Api.Contracts.Bookings;

public sealed class CancelBookingApiRequest
{
    public string? Reason { get; init; }
}
