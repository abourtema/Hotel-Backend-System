namespace HotelResortBooking.Domain.Entities;

public sealed class RoomTypeUpgradeOption
{
    public Guid RoomTypeId { get; set; }

    public Guid UpgradeOptionId { get; set; }

    public RoomType RoomType { get; set; } = null!;

    public UpgradeOption UpgradeOption { get; set; } = null!;
}
