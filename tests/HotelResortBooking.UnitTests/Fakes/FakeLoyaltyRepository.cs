using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.UnitTests.Fakes;

public sealed class FakeLoyaltyRepository : ILoyaltyRepository
{
    private readonly Dictionary<Guid, LoyaltyAccountData> _accounts = new();
    private readonly List<LoyaltyTransactionData> _transactions = [];

    public void SeedBalance(Guid userId, int balance, int lifetimeEarned = 0, int lifetimeRedeemed = 0)
    {
        _accounts[userId] = new LoyaltyAccountData
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CurrentBalance = balance,
            LifetimeEarned = lifetimeEarned,
            LifetimeRedeemed = lifetimeRedeemed
        };
    }

    public Task<LoyaltyAccountData> GetOrCreateAccountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (!_accounts.TryGetValue(userId, out var account))
        {
            account = new LoyaltyAccountData
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CurrentBalance = 0,
                LifetimeEarned = 0,
                LifetimeRedeemed = 0
            };
            _accounts[userId] = account;
        }

        return Task.FromResult(Clone(account));
    }

    public Task<LoyaltyAccountData?> GetAccountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(_accounts.TryGetValue(userId, out var account) ? Clone(account) : null);

    public Task<IReadOnlyList<LoyaltyTransactionData>> GetTransactionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (!_accounts.TryGetValue(userId, out var account))
        {
            return Task.FromResult<IReadOnlyList<LoyaltyTransactionData>>([]);
        }

        var items = _transactions.Where(t => t.LoyaltyAccountId == account.Id).ToList();
        return Task.FromResult<IReadOnlyList<LoyaltyTransactionData>>(items);
    }

    public Task RedeemAsync(
        Guid userId,
        Guid bookingId,
        int points,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var account = EnsureAccount(userId);
        if (account.CurrentBalance < points)
        {
            throw new InvalidOperationException("Insufficient loyalty points.");
        }

        account = new LoyaltyAccountData
        {
            Id = account.Id,
            UserId = account.UserId,
            CurrentBalance = account.CurrentBalance - points,
            LifetimeEarned = account.LifetimeEarned,
            LifetimeRedeemed = account.LifetimeRedeemed + points
        };
        _accounts[userId] = account;

        _transactions.Add(new LoyaltyTransactionData
        {
            Id = Guid.NewGuid(),
            LoyaltyAccountId = account.Id,
            BookingId = bookingId,
            TransactionType = LoyaltyTransactionType.Redeem,
            Points = -points,
            BalanceAfter = account.CurrentBalance,
            Description = description,
            CreatedAt = DateTimeOffset.UtcNow
        });

        return Task.CompletedTask;
    }

    public Task RestoreAsync(
        Guid userId,
        Guid bookingId,
        int points,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var account = EnsureAccount(userId);
        account = new LoyaltyAccountData
        {
            Id = account.Id,
            UserId = account.UserId,
            CurrentBalance = account.CurrentBalance + points,
            LifetimeEarned = account.LifetimeEarned,
            LifetimeRedeemed = Math.Max(0, account.LifetimeRedeemed - points)
        };
        _accounts[userId] = account;

        _transactions.Add(new LoyaltyTransactionData
        {
            Id = Guid.NewGuid(),
            LoyaltyAccountId = account.Id,
            BookingId = bookingId,
            TransactionType = LoyaltyTransactionType.Refund,
            Points = points,
            BalanceAfter = account.CurrentBalance,
            Description = description,
            CreatedAt = DateTimeOffset.UtcNow
        });

        return Task.CompletedTask;
    }

    public Task<LoyaltyAccountData> AdjustAsync(
        Guid userId,
        int pointsDelta,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var account = EnsureAccount(userId);
        var next = account.CurrentBalance + pointsDelta;
        if (next < 0)
        {
            throw new InvalidOperationException("Loyalty balance cannot be negative.");
        }

        account = new LoyaltyAccountData
        {
            Id = account.Id,
            UserId = account.UserId,
            CurrentBalance = next,
            LifetimeEarned = account.LifetimeEarned + Math.Max(0, pointsDelta),
            LifetimeRedeemed = account.LifetimeRedeemed
        };
        _accounts[userId] = account;

        _transactions.Add(new LoyaltyTransactionData
        {
            Id = Guid.NewGuid(),
            LoyaltyAccountId = account.Id,
            TransactionType = LoyaltyTransactionType.Adjustment,
            Points = pointsDelta,
            BalanceAfter = account.CurrentBalance,
            Description = description,
            CreatedAt = DateTimeOffset.UtcNow
        });

        return Task.FromResult(Clone(account));
    }

    private LoyaltyAccountData EnsureAccount(Guid userId)
    {
        if (!_accounts.TryGetValue(userId, out var account))
        {
            account = new LoyaltyAccountData
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                CurrentBalance = 0,
                LifetimeEarned = 0,
                LifetimeRedeemed = 0
            };
            _accounts[userId] = account;
        }

        return account;
    }

    private static LoyaltyAccountData Clone(LoyaltyAccountData account) => new()
    {
        Id = account.Id,
        UserId = account.UserId,
        CurrentBalance = account.CurrentBalance,
        LifetimeEarned = account.LifetimeEarned,
        LifetimeRedeemed = account.LifetimeRedeemed
    };
}
