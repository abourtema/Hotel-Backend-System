namespace HotelResortBooking.Application.UseCases.Loyalty;

public sealed class CalculatePointsRedemptionRequest
{
    public int Points { get; init; }

    public decimal BookingTotalBeforePoints { get; init; }

    public decimal PointValue { get; init; } = 0.01m;
}
