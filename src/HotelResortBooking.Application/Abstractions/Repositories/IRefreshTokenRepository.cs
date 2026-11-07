namespace HotelResortBooking.Application.Abstractions.Repositories;

public sealed class RefreshTokenData
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }

    public Guid? ReplacedByTokenId { get; set; }

    public string? CreatedByIp { get; set; }

    public string? RevokedByIp { get; set; }

    public bool IsRevoked => RevokedAt.HasValue;
}

public interface IRefreshTokenRepository
{
    Task<RefreshTokenData?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task AddAsync(RefreshTokenData token, CancellationToken cancellationToken = default);

    Task UpdateAsync(RefreshTokenData token, CancellationToken cancellationToken = default);

    Task RevokeAllForUserAsync(Guid userId, DateTimeOffset revokedAt, string? revokedByIp, CancellationToken cancellationToken = default);
}
