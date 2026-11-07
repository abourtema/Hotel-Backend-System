namespace HotelResortBooking.Application.UseCases.Loyalty;

public sealed class CalculatePointsRedemptionResponse
{
    public int RequestedPoints { get; init; }

    public int ApplicablePoints { get; init; }

    public decimal PointsDiscount { get; init; }

    public decimal RemainingPayable { get; init; }

    public int AvailableBalance { get; init; }
}
