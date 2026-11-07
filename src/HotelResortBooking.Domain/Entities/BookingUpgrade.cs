namespace HotelResortBooking.Domain.Entities;

using HotelResortBooking.Domain.Common;

public sealed class BookingUpgrade : Entity
{
    public Guid BookingId { get; set; }

    public Guid UpgradeOptionId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Booking Booking { get; set; } = null!;

    public UpgradeOption UpgradeOption { get; set; } = null!;
}
