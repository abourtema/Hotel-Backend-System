using HotelResortBooking.Application.Abstractions;

namespace HotelResortBooking.Application.UseCases.Auth;

public sealed class LogoutUserResponse
{
    public bool Success { get; init; } = true;

    public AccessMode AccessMode { get; init; } = AccessMode.Initial;
}
