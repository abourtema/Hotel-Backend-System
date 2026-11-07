namespace HotelResortBooking.Domain.Entities;

using HotelResortBooking.Domain.Common;

public sealed class RoomInventory : AuditableEntity
{
    public Guid RoomTypeId { get; set; }

    public DateOnly Date { get; set; }

    public int TotalRooms { get; set; }

    public int AvailableRooms { get; set; }

    public RoomType RoomType { get; set; } = null!;
}
