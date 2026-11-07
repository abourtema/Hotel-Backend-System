namespace HotelResortBooking.Application.UseCases.Loyalty;

public sealed class RedeemPointsRequest
{
    public Guid BookingId { get; init; }

    public int Points { get; init; }

    public string? Description { get; init; }
}
