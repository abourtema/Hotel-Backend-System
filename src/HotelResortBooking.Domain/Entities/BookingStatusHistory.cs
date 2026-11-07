namespace HotelResortBooking.Domain.Entities;

using HotelResortBooking.Domain.Common;
using HotelResortBooking.Domain.Enums;

public sealed class BookingStatusHistory : Entity
{
    public Guid BookingId { get; set; }

    public BookingStatus? OldStatus { get; set; }

    public BookingStatus NewStatus { get; set; }

    public DateTimeOffset ChangedAt { get; set; }

    public string? Reason { get; set; }

    public Booking Booking { get; set; } = null!;
}
