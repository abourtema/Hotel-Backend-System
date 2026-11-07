using HotelResortBooking.Application.UseCases.Bookings;

namespace HotelResortBooking.Application.UseCases.Loyalty;

public sealed class ApplyLoyaltyPointsToBookingResponse
{
    public BookingDto Booking { get; init; } = null!;

    public int PointsApplied { get; init; }

    public decimal PointsDiscount { get; init; }
}
