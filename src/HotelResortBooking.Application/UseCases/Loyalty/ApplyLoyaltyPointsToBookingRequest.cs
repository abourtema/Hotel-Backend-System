namespace HotelResortBooking.Application.UseCases.Loyalty;

public sealed class ApplyLoyaltyPointsToBookingRequest
{
    public Guid BookingId { get; init; }

    public int Points { get; init; }
}
