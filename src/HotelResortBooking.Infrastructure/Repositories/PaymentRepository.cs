using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Domain.Entities;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelResortBooking.Infrastructure.Repositories;

public sealed class PaymentRepository : IPaymentRepository
{
    private readonly ApplicationDbContext _dbContext;

    public PaymentRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PaymentData?> GetByIdAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        var payment = await _dbContext.Payments.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

        return payment is null ? null : Map(payment);
    }

    public async Task<IReadOnlyList<PaymentData>> GetByBookingIdAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        var payments = await _dbContext.Payments.AsNoTracking()
            .Where(p => p.BookingId == bookingId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return payments.Select(Map).ToList();
    }

    public async Task<IReadOnlyList<PaymentData>> GetByBookingIdsAsync(
        IReadOnlyCollection<Guid> bookingIds,
        CancellationToken cancellationToken = default)
    {
        if (bookingIds.Count == 0)
        {
            return Array.Empty<PaymentData>();
        }

        var payments = await _dbContext.Payments.AsNoTracking()
            .Where(p => bookingIds.Contains(p.BookingId))
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

        return payments.Select(Map).ToList();
    }

    public async Task<PaymentData?> GetByProviderTransactionIdAsync(
        string providerTransactionId,
        CancellationToken cancellationToken = default)
    {
        var payment = await _dbContext.Payments.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProviderTransactionId == providerTransactionId, cancellationToken);

        return payment is null ? null : Map(payment);
    }

    public async Task<PaymentData?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        var payment = await _dbContext.Payments.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ProviderTransactionId == idempotencyKey, cancellationToken);

        return payment is null ? null : Map(payment);
    }

    public async Task<PaymentData> AddAsync(
        CreatePaymentModel model,
        CancellationToken cancellationToken = default)
    {
        var providerTransactionId = model.ProviderTransactionId;
        if (string.IsNullOrWhiteSpace(providerTransactionId)
            && !string.IsNullOrWhiteSpace(model.IdempotencyKey))
        {
            providerTransactionId = model.IdempotencyKey;
        }

        var payment = new Payment
        {
            BookingId = model.BookingId,
            UserId = model.UserId,
            Amount = model.Amount,
            Currency = model.Currency,
            PaymentMethod = model.PaymentMethod,
            Status = PaymentStatus.Pending,
            Provider = model.Provider,
            ProviderTransactionId = providerTransactionId,
            PaymentMethodToken = model.PaymentMethodToken,
            CardBrand = model.CardBrand,
            CardLast4 = model.CardLast4
        };

        _dbContext.Payments.Add(payment);
        await Task.CompletedTask;
        return Map(payment);
    }

    public async Task UpdateAsync(PaymentData payment, CancellationToken cancellationToken = default)
    {
        var entity = _dbContext.Payments.Local.FirstOrDefault(p => p.Id == payment.Id)
            ?? await _dbContext.Payments.FirstOrDefaultAsync(p => p.Id == payment.Id, cancellationToken);

        if (entity is null)
        {
            throw new InvalidOperationException($"Payment '{payment.Id}' was not found.");
        }

        entity.Amount = payment.Amount;
        entity.Currency = payment.Currency;
        entity.PaymentMethod = payment.PaymentMethod;
        entity.Status = payment.Status;
        entity.Provider = payment.Provider;
        entity.ProviderTransactionId = payment.ProviderTransactionId;
        entity.PaymentMethodToken = payment.PaymentMethodToken;
        entity.CardBrand = payment.CardBrand;
        entity.CardLast4 = payment.CardLast4;
        entity.FailureReason = payment.FailureReason;
        entity.PaidAt = payment.PaidAt;
    }

    private static PaymentData Map(Payment payment) => new()
    {
        Id = payment.Id,
        BookingId = payment.BookingId,
        UserId = payment.UserId,
        Amount = payment.Amount,
        Currency = payment.Currency,
        PaymentMethod = payment.PaymentMethod,
        Status = payment.Status,
        Provider = payment.Provider,
        ProviderTransactionId = payment.ProviderTransactionId,
        PaymentMethodToken = payment.PaymentMethodToken,
        CardBrand = payment.CardBrand,
        CardLast4 = payment.CardLast4,
        FailureReason = payment.FailureReason,
        PaidAt = payment.PaidAt,
        CreatedAt = payment.CreatedAt
    };
}
