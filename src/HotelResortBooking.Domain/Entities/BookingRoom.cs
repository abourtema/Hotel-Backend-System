namespace HotelResortBooking.Domain.Entities;

using HotelResortBooking.Domain.Common;

public sealed class BookingRoom : Entity
{
    public Guid BookingId { get; set; }

    public Guid RoomTypeId { get; set; }

    public int Quantity { get; set; }

    public decimal PricePerNight { get; set; }

    public int Nights { get; set; }

    public decimal SubTotal { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Booking Booking { get; set; } = null!;

    public RoomType RoomType { get; set; } = null!;
}
