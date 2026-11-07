using HotelResortBooking.Application.Abstractions;

namespace HotelResortBooking.Application.UseCases.Reviews;

public sealed class AddReviewRequest
{
    public Guid BookingId { get; init; }

    public int Rating { get; init; }

    public string? Comment { get; init; }

    public IReadOnlyCollection<string> Tags { get; init; } = Array.Empty<string>();

    public IReadOnlyList<UploadedImageFile> Photos { get; init; } = [];
}
