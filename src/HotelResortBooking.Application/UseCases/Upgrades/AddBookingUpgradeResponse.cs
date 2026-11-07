using HotelResortBooking.Application.UseCases.Bookings;

namespace HotelResortBooking.Application.UseCases.Upgrades;

public sealed class AddBookingUpgradeResponse
{
    public BookingDto Booking { get; init; } = null!;
}
