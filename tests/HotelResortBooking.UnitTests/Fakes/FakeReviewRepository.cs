using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.UnitTests.Fakes;

public sealed class FakeReviewRepository : IReviewRepository
{
    public List<ReviewData> Reviews { get; } = [];

    public Task<bool> ExistsForBookingAsync(Guid bookingId, Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(Reviews.Any(r => r.BookingId == bookingId && r.UserId == userId));

    public Task<ReviewData> AddAsync(
        Guid userId,
        Guid propertyId,
        Guid bookingId,
        int rating,
        string? comment,
        IReadOnlyCollection<string> tags,
        IReadOnlyCollection<string> photoUrls,
        CancellationToken cancellationToken = default)
    {
        var review = new ReviewData
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            PropertyId = propertyId,
            BookingId = bookingId,
            Rating = rating,
            Comment = comment,
            Status = ReviewStatus.Pending,
            Tags = tags.ToList(),
            PhotoUrls = photoUrls.ToList()
        };
        Reviews.Add(review);
        return Task.FromResult(review);
    }
}
