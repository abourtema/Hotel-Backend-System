using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Security;
using HotelResortBooking.Application.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HotelResortBooking.Infrastructure.Identity;

public sealed class JwtTokenService : ITokenService
{
    private readonly JwtOptions _options;
    private readonly IDateTimeProvider _clock;

    public JwtTokenService(IOptions<JwtOptions> options, IDateTimeProvider clock)
    {
        _options = options.Value;
        _clock = clock;
    }

    public AccessTokenResult CreateAccessToken(Guid userId, string email, string role)
        => CreateAccessToken(userId, email, role, Array.Empty<string>(), Array.Empty<string>());

    public AccessTokenResult CreateAccessToken(
        Guid userId,
        string email,
        string role,
        IReadOnlyCollection<string> additionalRoles,
        IReadOnlyCollection<string> permissions)
    {
        var jwtId = Guid.NewGuid().ToString("N");
        var expiresAt = _clock.UtcNow.AddMinutes(Math.Max(1, _options.AccessTokenExpirationMinutes));
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));

        var primaryRole = string.IsNullOrWhiteSpace(role) ? Roles.User : role;
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Role, primaryRole),
            new(JwtRegisteredClaimNames.Jti, jwtId),
            new("token_type", "access")
        };

        foreach (var extraRole in additionalRoles.Where(r =>
                     !string.IsNullOrWhiteSpace(r)
                     && !string.Equals(r, primaryRole, StringComparison.OrdinalIgnoreCase)))
        {
            claims.Add(new Claim(ClaimTypes.Role, extraRole));
        }

        foreach (var permission in permissions.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim(Permissions.ClaimType, permission));
        }

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: _clock.UtcNow.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new AccessTokenResult
        {
            Token = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAt = expiresAt,
            JwtId = jwtId
        };
    }

    public RefreshTokenValue CreateRefreshToken()
    {
        var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        return new RefreshTokenValue
        {
            RawToken = rawToken,
            Hash = HashToken(rawToken),
            ExpiresAt = _clock.UtcNow.AddDays(Math.Max(1, _options.RefreshTokenExpirationDays))
        };
    }

    public string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes);
    }
}
