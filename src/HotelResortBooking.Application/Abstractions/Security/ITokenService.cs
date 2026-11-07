namespace HotelResortBooking.Application.Abstractions.Security;

public sealed class AccessTokenResult
{
    public string Token { get; init; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; init; }

    public string JwtId { get; init; } = string.Empty;
}

public sealed class RefreshTokenValue
{
    public string RawToken { get; init; } = string.Empty;

    public string Hash { get; init; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; init; }
}

public interface ITokenService
{
    AccessTokenResult CreateAccessToken(Guid userId, string email, string role);

    AccessTokenResult CreateAccessToken(
        Guid userId,
        string email,
        string role,
        IReadOnlyCollection<string> additionalRoles,
        IReadOnlyCollection<string> permissions)
        => CreateAccessToken(userId, email, role);

    RefreshTokenValue CreateRefreshToken();

    string HashToken(string rawToken);
}
