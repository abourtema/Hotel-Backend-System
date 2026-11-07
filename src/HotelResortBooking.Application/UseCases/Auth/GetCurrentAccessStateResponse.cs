using HotelResortBooking.Application.Abstractions;

namespace HotelResortBooking.Application.UseCases.Auth;

public sealed class GetCurrentAccessStateResponse
{
    public AccessMode AccessMode { get; init; }

    public Guid? UserId { get; init; }

    public bool IsAuthenticated { get; init; }
}
