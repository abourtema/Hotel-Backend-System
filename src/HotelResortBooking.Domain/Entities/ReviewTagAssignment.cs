namespace HotelResortBooking.Domain.Entities;

public sealed class ReviewTagAssignment
{
    public Guid ReviewId { get; set; }

    public Guid ReviewTagId { get; set; }

    public Review Review { get; set; } = null!;

    public ReviewTag ReviewTag { get; set; } = null!;
}
