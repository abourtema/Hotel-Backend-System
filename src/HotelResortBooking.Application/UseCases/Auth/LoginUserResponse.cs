using HotelResortBooking.Application.Abstractions;

namespace HotelResortBooking.Application.UseCases.Auth;

public sealed class LoginUserResponse
{
    public Guid UserId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public AccessMode AccessMode { get; init; } = AccessMode.Authenticated;
}
