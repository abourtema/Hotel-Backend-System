using HotelResortBooking.Application.Abstractions.Repositories;

namespace HotelResortBooking.UnitTests.Fakes;

public sealed class FakePaymentMethodRepository : IPaymentMethodRepository
{
    public List<SavedPaymentMethodData> Methods { get; } = [];

    public Task<SavedPaymentMethodData?> GetByIdAsync(
        Guid paymentMethodId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Methods.FirstOrDefault(m => m.Id == paymentMethodId));

    public Task<IReadOnlyList<SavedPaymentMethodData>> GetByUserIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<SavedPaymentMethodData>>(
            Methods.Where(m => m.UserId == userId && m.IsActive).ToList());
}
