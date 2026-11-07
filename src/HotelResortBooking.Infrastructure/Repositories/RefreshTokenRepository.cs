using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Domain.Entities;
using HotelResortBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelResortBooking.Infrastructure.Repositories;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ApplicationDbContext _dbContext;

    public RefreshTokenRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RefreshTokenData?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        return entity is null ? null : Map(entity);
    }

    public async Task AddAsync(RefreshTokenData token, CancellationToken cancellationToken = default)
    {
        _dbContext.RefreshTokens.Add(new RefreshToken
        {
            Id = token.Id == Guid.Empty ? Guid.NewGuid() : token.Id,
            UserId = token.UserId,
            TokenHash = token.TokenHash,
            ExpiresAt = token.ExpiresAt,
            CreatedAt = token.CreatedAt,
            RevokedAt = token.RevokedAt,
            ReplacedByTokenId = token.ReplacedByTokenId,
            CreatedByIp = token.CreatedByIp,
            RevokedByIp = token.RevokedByIp
        });

        await Task.CompletedTask;
    }

    public async Task UpdateAsync(RefreshTokenData token, CancellationToken cancellationToken = default)
    {
        var entity = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(x => x.Id == token.Id, cancellationToken);

        if (entity is null)
        {
            return;
        }

        entity.RevokedAt = token.RevokedAt;
        entity.RevokedByIp = token.RevokedByIp;
        entity.ReplacedByTokenId = token.ReplacedByTokenId;
    }

    public async Task RevokeAllForUserAsync(
        Guid userId,
        DateTimeOffset revokedAt,
        string? revokedByIp,
        CancellationToken cancellationToken = default)
    {
        var tokens = await _dbContext.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.RevokedAt = revokedAt;
            token.RevokedByIp = revokedByIp;
        }
    }

    private static RefreshTokenData Map(RefreshToken entity) => new()
    {
        Id = entity.Id,
        UserId = entity.UserId,
        TokenHash = entity.TokenHash,
        ExpiresAt = entity.ExpiresAt,
        CreatedAt = entity.CreatedAt,
        RevokedAt = entity.RevokedAt,
        ReplacedByTokenId = entity.ReplacedByTokenId,
        CreatedByIp = entity.CreatedByIp,
        RevokedByIp = entity.RevokedByIp
    };
}
