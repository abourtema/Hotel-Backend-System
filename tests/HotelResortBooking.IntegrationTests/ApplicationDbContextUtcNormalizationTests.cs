using HotelResortBooking.Domain.Entities;
using HotelResortBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelResortBooking.IntegrationTests;

public sealed class ApplicationDbContextUtcNormalizationTests
{
    [Fact]
    public async Task SaveChanges_RewritesNonUtcDateTimeOffsetToUtcInstant()
    {
        await using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"utc-{Guid.NewGuid():N}")
                .Options);

        var local = new DateTimeOffset(2026, 9, 7, 0, 0, 0, TimeSpan.FromHours(2));
        db.Amenities.Add(new Amenity
        {
            Name = $"Pool-{Guid.NewGuid():N}",
            CreatedAt = local
        });

        await db.SaveChangesAsync();

        var saved = await db.Amenities.AsNoTracking().SingleAsync();
        Assert.Equal(TimeSpan.Zero, saved.CreatedAt.Offset);
        Assert.Equal(new DateTimeOffset(2026, 9, 6, 22, 0, 0, TimeSpan.Zero), saved.CreatedAt);
    }
}
