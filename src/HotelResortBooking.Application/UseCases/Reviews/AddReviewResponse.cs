using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.UseCases.Reviews;

public sealed class AddReviewResponse
{
    public Guid ReviewId { get; init; }

    public Guid BookingId { get; init; }

    public Guid PropertyId { get; init; }

    public int Rating { get; init; }

    public string? Comment { get; init; }

    public ReviewStatus Status { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> PhotoUrls { get; init; } = Array.Empty<string>();
}
