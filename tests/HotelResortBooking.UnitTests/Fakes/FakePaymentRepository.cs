using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.UnitTests.Fakes;

public sealed class FakePaymentRepository : IPaymentRepository
{
    public List<PaymentData> Payments { get; } = [];
    private readonly Dictionary<string, Guid> _idempotencyKeys = new(StringComparer.Ordinal);

    public Task<PaymentData?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken = default)
        => Task.FromResult(Payments.FirstOrDefault(p => p.Id == paymentId));

    public Task<IReadOnlyList<PaymentData>> GetByBookingIdAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PaymentData>>(
            Payments.Where(p => p.BookingId == bookingId).ToList());

    public Task<IReadOnlyList<PaymentData>> GetByBookingIdsAsync(
        IReadOnlyCollection<Guid> bookingIds,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PaymentData>>(
            Payments.Where(p => bookingIds.Contains(p.BookingId)).ToList());

    public Task<PaymentData?> GetByProviderTransactionIdAsync(
        string providerTransactionId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Payments.FirstOrDefault(p =>
            string.Equals(p.ProviderTransactionId, providerTransactionId, StringComparison.Ordinal)));

    public Task<PaymentData?> GetByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        if (_idempotencyKeys.TryGetValue(idempotencyKey, out var paymentId))
        {
            return Task.FromResult(Payments.FirstOrDefault(p => p.Id == paymentId));
        }

        // Also allow matching via ProviderTransactionId for seeded success payments.
        return GetByProviderTransactionIdAsync(idempotencyKey, cancellationToken);
    }

    public Task<PaymentData> AddAsync(CreatePaymentModel model, CancellationToken cancellationToken = default)
    {
        var payment = new PaymentData
        {
            Id = Guid.NewGuid(),
            BookingId = model.BookingId,
            UserId = model.UserId,
            Amount = model.Amount,
            Currency = model.Currency,
            PaymentMethod = model.PaymentMethod,
            Status = PaymentStatus.Pending,
            Provider = model.Provider,
            ProviderTransactionId = model.ProviderTransactionId,
            PaymentMethodToken = model.PaymentMethodToken,
            CardBrand = model.CardBrand,
            CardLast4 = model.CardLast4,
            CreatedAt = DateTimeOffset.UtcNow
        };

        Payments.Add(payment);

        if (!string.IsNullOrWhiteSpace(model.IdempotencyKey))
        {
            _idempotencyKeys[model.IdempotencyKey] = payment.Id;
        }

        return Task.FromResult(payment);
    }

    public Task UpdateAsync(PaymentData payment, CancellationToken cancellationToken = default)
    {
        var index = Payments.FindIndex(p => p.Id == payment.Id);
        if (index >= 0)
        {
            Payments[index] = payment;
        }
        else
        {
            Payments.Add(payment);
        }

        return Task.CompletedTask;
    }

    public void Seed(PaymentData payment, string? idempotencyKey = null)
    {
        Payments.Add(payment);
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            _idempotencyKeys[idempotencyKey] = payment.Id;
        }
        else if (!string.IsNullOrWhiteSpace(payment.ProviderTransactionId))
        {
            _idempotencyKeys[payment.ProviderTransactionId] = payment.Id;
        }
    }
}
