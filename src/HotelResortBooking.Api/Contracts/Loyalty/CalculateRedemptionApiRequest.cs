using HotelResortBooking.Application.UseCases.Loyalty;

namespace HotelResortBooking.Api.Contracts.Loyalty;

public sealed class CalculateRedemptionApiRequest
{
    public int Points { get; init; }

    public decimal BookingTotalBeforePoints { get; init; }

    public decimal PointValue { get; init; } = 0.01m;

    public CalculatePointsRedemptionRequest ToUseCaseRequest() => new()
    {
        Points = Points,
        BookingTotalBeforePoints = BookingTotalBeforePoints,
        PointValue = PointValue
    };
}
