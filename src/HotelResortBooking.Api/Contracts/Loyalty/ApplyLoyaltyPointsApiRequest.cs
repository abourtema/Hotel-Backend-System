using HotelResortBooking.Application.UseCases.Loyalty;

namespace HotelResortBooking.Api.Contracts.Loyalty;

public sealed class ApplyLoyaltyPointsApiRequest
{
    public Guid BookingId { get; init; }

    public int Points { get; init; }

    public ApplyLoyaltyPointsToBookingRequest ToUseCaseRequest() => new()
    {
        BookingId = BookingId,
        Points = Points
    };
}
