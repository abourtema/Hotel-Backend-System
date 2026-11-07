using HotelResortBooking.Application.Abstractions;

namespace HotelResortBooking.Application.UseCases.Auth;

public sealed class ContinueAsGuestResponse
{
    public AccessMode AccessMode { get; init; } = AccessMode.Guest;
}
