using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Domain.Entities;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelResortBooking.Infrastructure.Repositories;

public sealed class ReviewRepository : IReviewRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ReviewRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsForBookingAsync(
        Guid bookingId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Reviews.AsNoTracking()
            .AnyAsync(r => r.BookingId == bookingId && r.UserId == userId, cancellationToken);
    }

    public async Task<ReviewData> AddAsync(
        Guid userId,
        Guid propertyId,
        Guid bookingId,
        int rating,
        string? comment,
        IReadOnlyCollection<string> tags,
        IReadOnlyCollection<string> photoUrls,
        CancellationToken cancellationToken = default)
    {
        var review = new Review
        {
            UserId = userId,
            PropertyId = propertyId,
            BookingId = bookingId,
            Rating = rating,
            Comment = comment,
            Status = ReviewStatus.Pending
        };

        var tagNames = tags
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(t => t.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var tagName in tagNames)
        {
            var existingTag = await _dbContext.ReviewTags
                .FirstOrDefaultAsync(t => t.Name.ToLower() == tagName.ToLower(), cancellationToken);

            if (existingTag is null)
            {
                existingTag = new ReviewTag
                {
                    Name = tagName,
                    IsActive = true
                };
                _dbContext.ReviewTags.Add(existingTag);
            }

            review.TagAssignments.Add(new ReviewTagAssignment
            {
                Review = review,
                ReviewTag = existingTag
            });
        }

        var sortOrder = 0;
        foreach (var url in photoUrls.Where(u => !string.IsNullOrWhiteSpace(u)))
        {
            review.Images.Add(new ReviewImage
            {
                ImageUrl = url.Trim(),
                SortOrder = sortOrder++,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        _dbContext.Reviews.Add(review);

        return new ReviewData
        {
            Id = review.Id,
            UserId = review.UserId,
            PropertyId = review.PropertyId,
            BookingId = review.BookingId,
            Rating = review.Rating,
            Comment = review.Comment,
            Status = review.Status,
            Tags = tagNames,
            PhotoUrls = review.Images.OrderBy(i => i.SortOrder).Select(i => i.ImageUrl).ToList()
        };
    }
}
