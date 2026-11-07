namespace HotelResortBooking.Application.UseCases.Upgrades;

public sealed class GetAvailableUpgradesRequest
{
    public Guid PropertyId { get; init; }

    public Guid? RoomTypeId { get; init; }
}
