using HotelResortBooking.Application.Abstractions.Security;

namespace HotelResortBooking.UnitTests.Fakes;

public sealed class FakeTokenService : ITokenService
{
    public AccessTokenResult CreateAccessToken(Guid userId, string email, string role)
        => new()
        {
            Token = $"access:{userId}:{email}:{role}",
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
            JwtId = Guid.NewGuid().ToString("N")
        };

    public RefreshTokenValue CreateRefreshToken()
    {
        var raw = Guid.NewGuid().ToString("N");
        return new RefreshTokenValue
        {
            RawToken = raw,
            Hash = HashToken(raw),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(30)
        };
    }

    public string HashToken(string rawToken) => $"hash:{rawToken}";
}
