using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Domain.Entities;
using HotelResortBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelResortBooking.Infrastructure.Repositories;

public sealed class PaymentMethodRepository : IPaymentMethodRepository
{
    private readonly ApplicationDbContext _dbContext;

    public PaymentMethodRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SavedPaymentMethodData?> GetByIdAsync(
        Guid paymentMethodId,
        CancellationToken cancellationToken = default)
    {
        var method = await _dbContext.PaymentMethods.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == paymentMethodId, cancellationToken);

        return method is null ? null : Map(method);
    }

    public async Task<IReadOnlyList<SavedPaymentMethodData>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var methods = await _dbContext.PaymentMethods.AsNoTracking()
            .Where(m => m.UserId == userId && m.IsActive)
            .OrderByDescending(m => m.IsDefault)
            .ThenByDescending(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        return methods.Select(Map).ToList();
    }

    private static SavedPaymentMethodData Map(PaymentMethod method) => new()
    {
        Id = method.Id,
        UserId = method.UserId,
        Provider = method.Provider,
        ProviderPaymentMethodId = method.ProviderPaymentMethodId,
        CardBrand = method.CardBrand,
        CardLast4 = method.CardLast4,
        IsDefault = method.IsDefault,
        IsActive = method.IsActive
    };
}
