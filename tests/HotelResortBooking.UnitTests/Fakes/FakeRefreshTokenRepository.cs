using HotelResortBooking.Application.Abstractions.Repositories;

namespace HotelResortBooking.UnitTests.Fakes;

public sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshTokenData> Tokens { get; } = [];

    public Task<RefreshTokenData?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
        => Task.FromResult(Tokens.FirstOrDefault(t => t.TokenHash == tokenHash));

    public Task AddAsync(RefreshTokenData token, CancellationToken cancellationToken = default)
    {
        Tokens.Add(token);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(RefreshTokenData token, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RevokeAllForUserAsync(
        Guid userId,
        DateTimeOffset revokedAt,
        string? revokedByIp,
        CancellationToken cancellationToken = default)
    {
        foreach (var token in Tokens.Where(t => t.UserId == userId && t.RevokedAt is null))
        {
            token.RevokedAt = revokedAt;
            token.RevokedByIp = revokedByIp;
        }

        return Task.CompletedTask;
    }
}
