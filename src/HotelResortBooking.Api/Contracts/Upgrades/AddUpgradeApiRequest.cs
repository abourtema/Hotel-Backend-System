namespace HotelResortBooking.Api.Contracts.Upgrades;

public sealed class AddUpgradeApiRequest
{
    public Guid UpgradeOptionId { get; init; }

    public int Quantity { get; init; } = 1;
}
