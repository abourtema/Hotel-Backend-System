using HotelResortBooking.Application.Abstractions.Repositories;

namespace HotelResortBooking.Application.UseCases.Upgrades;

public sealed class GetAvailableUpgradesResponse
{
    public IReadOnlyList<UpgradeOptionData> Upgrades { get; init; } = Array.Empty<UpgradeOptionData>();
}
