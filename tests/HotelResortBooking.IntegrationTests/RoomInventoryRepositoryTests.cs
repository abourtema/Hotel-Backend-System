using HotelResortBooking.Domain.Entities;
using HotelResortBooking.Infrastructure.Persistence;
using HotelResortBooking.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HotelResortBooking.IntegrationTests;

public sealed class RoomInventoryRepositoryTests
{
    [Fact]
    public async Task CheckAfterUncommittedRelease_SeesFreedRooms()
    {
        await using var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase($"inventory-{Guid.NewGuid():N}")
                .Options);

        var roomTypeId = Guid.NewGuid();
        var checkIn = new DateOnly(2026, 8, 20);
        var checkOut = new DateOnly(2026, 8, 22);

        db.RoomInventories.AddRange(
            NewInventory(roomTypeId, checkIn, total: 1, available: 0),
            NewInventory(roomTypeId, checkIn.AddDays(1), total: 1, available: 0));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var repository = new RoomInventoryRepository(db);

        await repository.ReleaseAsync(roomTypeId, checkIn, checkOut, 1);

        var beforeSave = await repository.CheckAvailabilityAsync(roomTypeId, checkIn, checkOut, 1);
        Assert.True(beforeSave.IsAvailable);
        Assert.Equal(1, beforeSave.MinimumAvailableRooms);

        var reserved = await repository.ReserveAsync(roomTypeId, checkIn, checkOut, 1);
        Assert.True(reserved);
    }

    private static RoomInventory NewInventory(Guid roomTypeId, DateOnly date, int total, int available)
        => new()
        {
            RoomTypeId = roomTypeId,
            Date = date,
            TotalRooms = total,
            AvailableRooms = available
        };
}
