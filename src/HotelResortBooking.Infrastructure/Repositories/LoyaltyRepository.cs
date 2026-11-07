using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Domain.Entities;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelResortBooking.Infrastructure.Repositories;

public sealed class LoyaltyRepository : ILoyaltyRepository
{
    private readonly ApplicationDbContext _dbContext;

    public LoyaltyRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<LoyaltyAccountData> GetOrCreateAccountAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var account = await _dbContext.LoyaltyAccounts
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

        if (account is null)
        {
            account = new LoyaltyAccount
            {
                UserId = userId,
                CurrentBalance = 0,
                LifetimeEarned = 0,
                LifetimeRedeemed = 0
            };
            _dbContext.LoyaltyAccounts.Add(account);
        }

        return MapAccount(account);
    }

    public async Task<LoyaltyAccountData?> GetAccountByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var account = await _dbContext.LoyaltyAccounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

        return account is null ? null : MapAccount(account);
    }

    public async Task<IReadOnlyList<LoyaltyTransactionData>> GetTransactionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var account = await _dbContext.LoyaltyAccounts.AsNoTracking()
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

        if (account is null)
        {
            return Array.Empty<LoyaltyTransactionData>();
        }

        var transactions = await _dbContext.LoyaltyTransactions.AsNoTracking()
            .Where(t => t.LoyaltyAccountId == account.Id)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        return transactions.Select(MapTransaction).ToList();
    }

    public async Task RedeemAsync(
        Guid userId,
        Guid bookingId,
        int points,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var account = await _dbContext.LoyaltyAccounts
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

        if (account is null)
        {
            throw new InvalidOperationException($"Loyalty account for user '{userId}' was not found.");
        }

        if (account.CurrentBalance < points)
        {
            throw new InvalidOperationException("Insufficient loyalty points.");
        }

        account.CurrentBalance -= points;
        account.LifetimeRedeemed += points;

        _dbContext.LoyaltyTransactions.Add(new LoyaltyTransaction
        {
            LoyaltyAccountId = account.Id,
            BookingId = bookingId,
            TransactionType = LoyaltyTransactionType.Redeem,
            Points = points,
            BalanceAfter = account.CurrentBalance,
            Description = description,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    public async Task RestoreAsync(
        Guid userId,
        Guid bookingId,
        int points,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var account = await _dbContext.LoyaltyAccounts
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

        if (account is null)
        {
            throw new InvalidOperationException($"Loyalty account for user '{userId}' was not found.");
        }

        account.CurrentBalance += points;

        _dbContext.LoyaltyTransactions.Add(new LoyaltyTransaction
        {
            LoyaltyAccountId = account.Id,
            BookingId = bookingId,
            TransactionType = LoyaltyTransactionType.Refund,
            Points = points,
            BalanceAfter = account.CurrentBalance,
            Description = description,
            CreatedAt = DateTimeOffset.UtcNow
        });
    }

    public async Task<LoyaltyAccountData> AdjustAsync(
        Guid userId,
        int pointsDelta,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var account = await _dbContext.LoyaltyAccounts
            .FirstOrDefaultAsync(a => a.UserId == userId, cancellationToken);

        if (account is null)
        {
            account = new LoyaltyAccount { UserId = userId };
            _dbContext.LoyaltyAccounts.Add(account);
        }

        var next = account.CurrentBalance + pointsDelta;
        if (next < 0)
        {
            throw new InvalidOperationException("Loyalty balance cannot be negative.");
        }

        account.CurrentBalance = next;
        if (pointsDelta > 0)
        {
            account.LifetimeEarned += pointsDelta;
        }

        _dbContext.LoyaltyTransactions.Add(new LoyaltyTransaction
        {
            LoyaltyAccountId = account.Id,
            TransactionType = LoyaltyTransactionType.Adjustment,
            Points = pointsDelta,
            BalanceAfter = account.CurrentBalance,
            Description = description,
            CreatedAt = DateTimeOffset.UtcNow
        });

        return MapAccount(account);
    }

    private static LoyaltyAccountData MapAccount(LoyaltyAccount account) => new()
    {
        Id = account.Id,
        UserId = account.UserId,
        CurrentBalance = account.CurrentBalance,
        LifetimeEarned = account.LifetimeEarned,
        LifetimeRedeemed = account.LifetimeRedeemed
    };

    private static LoyaltyTransactionData MapTransaction(LoyaltyTransaction transaction) => new()
    {
        Id = transaction.Id,
        LoyaltyAccountId = transaction.LoyaltyAccountId,
        BookingId = transaction.BookingId,
        TransactionType = transaction.TransactionType,
        Points = transaction.Points,
        BalanceAfter = transaction.BalanceAfter,
        Description = transaction.Description,
        CreatedAt = transaction.CreatedAt
    };
}
