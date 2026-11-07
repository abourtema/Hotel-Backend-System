namespace HotelResortBooking.Domain.Entities;

using HotelResortBooking.Domain.Common;
using HotelResortBooking.Domain.Enums;

public sealed class UpgradeOption : AuditableEntity
{
    public Guid PropertyId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public UpgradeType UpgradeType { get; set; }

    public decimal Price { get; set; }

    public string Currency { get; set; } = string.Empty;

    public bool IsMandatory { get; set; }

    public bool IsRecommended { get; set; }

    public bool IsActive { get; set; } = true;

    public Property Property { get; set; } = null!;

    public ICollection<RoomTypeUpgradeOption> RoomTypeUpgradeOptions { get; set; } = new List<RoomTypeUpgradeOption>();

    public ICollection<BookingUpgrade> BookingUpgrades { get; set; } = new List<BookingUpgrade>();
}
