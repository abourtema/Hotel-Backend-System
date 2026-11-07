namespace HotelResortBooking.Domain.Entities;

using HotelResortBooking.Domain.Common;

public sealed class RoomType : AuditableEntity
{
    public Guid PropertyId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal? RoomSize { get; set; }

    public int MaxAdults { get; set; }

    public int MaxChildren { get; set; }

    public int MaxGuests { get; set; }

    public string? BedType { get; set; }

    public decimal BasePricePerNight { get; set; }

    public string Currency { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public Property Property { get; set; } = null!;

    public ICollection<RoomImage> Images { get; set; } = new List<RoomImage>();

    public ICollection<RoomTypeAmenity> RoomTypeAmenities { get; set; } = new List<RoomTypeAmenity>();

    public ICollection<RoomInventory> Inventories { get; set; } = new List<RoomInventory>();

    public ICollection<RoomTypeUpgradeOption> RoomTypeUpgradeOptions { get; set; } = new List<RoomTypeUpgradeOption>();

    public ICollection<BookingRoom> BookingRooms { get; set; } = new List<BookingRoom>();
}
