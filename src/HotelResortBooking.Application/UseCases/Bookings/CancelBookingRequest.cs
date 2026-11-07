namespace HotelResortBooking.Application.UseCases.Bookings;

public sealed class CancelBookingRequest
{
    public Guid BookingId { get; init; }

    public string? Reason { get; init; }

    public bool BypassOwnershipCheck { get; init; }
}
