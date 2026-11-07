namespace HotelResortBooking.Domain.Entities;

using HotelResortBooking.Domain.Common;
using HotelResortBooking.Domain.Enums;

public sealed class Booking : AggregateRoot
{
    public string BookingNumber { get; set; } = string.Empty;

    /// <summary>
    /// Null when the booking was created as a guest checkout.
    /// </summary>
    public Guid? UserId { get; set; }

    public Guid PropertyId { get; set; }

    public DateOnly CheckInDate { get; set; }

    public DateOnly CheckOutDate { get; set; }

    public int Adults { get; set; }

    public int Children { get; set; }

    public int RoomsCount { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;

    public string Currency { get; set; } = string.Empty;

    public decimal SubTotal { get; set; }

    public decimal UpgradeTotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public int PointsRedeemed { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public DateTimeOffset? CancellationDeadline { get; set; }

    public DateTimeOffset? ConfirmedAt { get; set; }

    public DateTimeOffset? CancelledAt { get; set; }

    /// <summary>
    /// Optional notes from the guest (early check-in, bed type, airport transfer, etc.).
    /// </summary>
    public string? SpecialRequests { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public User? User { get; set; }

    public Property Property { get; set; } = null!;

    public ICollection<BookingRoom> Rooms { get; set; } = new List<BookingRoom>();

    public ICollection<BookingGuest> Guests { get; set; } = new List<BookingGuest>();

    public ICollection<BookingUpgrade> Upgrades { get; set; } = new List<BookingUpgrade>();

    public ICollection<BookingStatusHistory> StatusHistory { get; set; } = new List<BookingStatusHistory>();

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public ICollection<LoyaltyTransaction> LoyaltyTransactions { get; set; } = new List<LoyaltyTransaction>();

    public Review? Review { get; set; }
}
