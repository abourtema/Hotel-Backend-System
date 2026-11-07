using HotelResortBooking.Application.UseCases.Bookings;

namespace HotelResortBooking.Application.UseCases.Upgrades;

public sealed class RemoveBookingUpgradeResponse
{
    public BookingDto Booking { get; init; } = null!;
}
