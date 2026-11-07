using HotelResortBooking.Application.Abstractions.Repositories;

namespace HotelResortBooking.UnitTests.Fakes;

public sealed class FakeUpgradeRepository : IUpgradeRepository
{
    public List<UpgradeOptionData> Upgrades { get; } = [];
    public Dictionary<Guid, Guid> UpgradePropertyMap { get; } = [];

    public Task<UpgradeOptionData?> GetByIdAsync(Guid upgradeOptionId, CancellationToken cancellationToken = default)
        => Task.FromResult(Upgrades.FirstOrDefault(u => u.Id == upgradeOptionId));

    public Task<IReadOnlyList<UpgradeOptionData>> GetForPropertyAsync(
        Guid propertyId,
        Guid? roomTypeId = null,
        CancellationToken cancellationToken = default)
    {
        var items = Upgrades
            .Where(u => !UpgradePropertyMap.TryGetValue(u.Id, out var pid) || pid == propertyId)
            .ToList();

        return Task.FromResult<IReadOnlyList<UpgradeOptionData>>(items);
    }
}
