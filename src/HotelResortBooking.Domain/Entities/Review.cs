namespace HotelResortBooking.Domain.Entities;

using HotelResortBooking.Domain.Common;
using HotelResortBooking.Domain.Enums;

public sealed class Review : AuditableEntity
{
    public Guid UserId { get; set; }

    public Guid PropertyId { get; set; }

    public Guid BookingId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public ReviewStatus Status { get; set; } = ReviewStatus.Pending;

    public User User { get; set; } = null!;

    public Property Property { get; set; } = null!;

    public Booking Booking { get; set; } = null!;

    public ICollection<ReviewImage> Images { get; set; } = new List<ReviewImage>();

    public ICollection<ReviewTagAssignment> TagAssignments { get; set; } = new List<ReviewTagAssignment>();
}
