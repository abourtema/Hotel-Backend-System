using HotelResortBooking.Application.UseCases.Loyalty;

namespace HotelResortBooking.Api.Contracts.Loyalty;

public sealed class RedeemPointsApiRequest
{
    public Guid BookingId { get; init; }

    public int Points { get; init; }

    public string? Description { get; init; }

    public RedeemPointsRequest ToUseCaseRequest() => new()
    {
        BookingId = BookingId,
        Points = Points,
        Description = Description
    };
}
