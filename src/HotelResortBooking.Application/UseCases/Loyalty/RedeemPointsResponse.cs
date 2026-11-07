namespace HotelResortBooking.Application.UseCases.Loyalty;

public sealed class RedeemPointsResponse
{
    public Guid BookingId { get; init; }

    public int PointsRedeemed { get; init; }

    public int RemainingBalance { get; init; }
}
