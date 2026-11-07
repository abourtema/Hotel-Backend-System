namespace HotelResortBooking.Application.UseCases.Loyalty;

public sealed class RefundPointsResponse
{
    public Guid BookingId { get; init; }

    public int PointsRefunded { get; init; }

    public int RemainingBalance { get; init; }
}
