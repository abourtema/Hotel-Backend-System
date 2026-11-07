using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.Abstractions.Repositories;

public sealed class LoyaltyAccountData
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public int CurrentBalance { get; init; }

    public int LifetimeEarned { get; init; }

    public int LifetimeRedeemed { get; init; }
}

public sealed class LoyaltyTransactionData
{
    public Guid Id { get; init; }

    public Guid LoyaltyAccountId { get; init; }

    public Guid? BookingId { get; init; }

    public LoyaltyTransactionType TransactionType { get; init; }

    public int Points { get; init; }

    public int BalanceAfter { get; init; }

    public string? Description { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public interface ILoyaltyRepository
{
    Task<LoyaltyAccountData> GetOrCreateAccountAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<LoyaltyAccountData?> GetAccountByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LoyaltyTransactionData>> GetTransactionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task RedeemAsync(
        Guid userId,
        Guid bookingId,
        int points,
        string? description,
        CancellationToken cancellationToken = default);

    Task RestoreAsync(
        Guid userId,
        Guid bookingId,
        int points,
        string? description,
        CancellationToken cancellationToken = default);

    Task<LoyaltyAccountData> AdjustAsync(
        Guid userId,
        int pointsDelta,
        string? description,
        CancellationToken cancellationToken = default);
}

public sealed class PaymentData
{
    public Guid Id { get; init; }

    public Guid BookingId { get; init; }

    public Guid? UserId { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; } = string.Empty;

    public PaymentMethodType PaymentMethod { get; init; }

    public PaymentStatus Status { get; init; }

    public string? Provider { get; init; }

    public string? ProviderTransactionId { get; init; }

    public string? PaymentMethodToken { get; init; }

    public string? CardBrand { get; init; }

    public string? CardLast4 { get; init; }

    public string? FailureReason { get; init; }

    public DateTimeOffset? PaidAt { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class CreatePaymentModel
{
    public Guid BookingId { get; init; }

    public Guid? UserId { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; } = string.Empty;

    public PaymentMethodType PaymentMethod { get; init; }

    public string? Provider { get; init; }

    public string? ProviderTransactionId { get; init; }

    public string? PaymentMethodToken { get; init; }

    public string? CardBrand { get; init; }

    public string? CardLast4 { get; init; }

    public string? IdempotencyKey { get; init; }
}

public sealed class SavedPaymentMethodData
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public string Provider { get; init; } = string.Empty;

    public string ProviderPaymentMethodId { get; init; } = string.Empty;

    public string? CardBrand { get; init; }

    public string? CardLast4 { get; init; }

    public bool IsDefault { get; init; }

    public bool IsActive { get; init; }
}

public interface IPaymentRepository
{
    Task<PaymentData?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentData>> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentData>> GetByBookingIdsAsync(
        IReadOnlyCollection<Guid> bookingIds,
        CancellationToken cancellationToken = default);

    Task<PaymentData?> GetByProviderTransactionIdAsync(
        string providerTransactionId,
        CancellationToken cancellationToken = default);

    Task<PaymentData?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<PaymentData> AddAsync(CreatePaymentModel model, CancellationToken cancellationToken = default);

    Task UpdateAsync(PaymentData payment, CancellationToken cancellationToken = default);
}

public interface IPaymentMethodRepository
{
    Task<SavedPaymentMethodData?> GetByIdAsync(Guid paymentMethodId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SavedPaymentMethodData>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}
