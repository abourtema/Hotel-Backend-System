using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelResortBooking.Infrastructure.Repositories;

public sealed class RoomInventoryRepository : IRoomInventoryRepository
{
    private readonly ApplicationDbContext _dbContext;

    public RoomInventoryRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<InventoryAvailabilityResult> CheckAvailabilityAsync(
        Guid roomTypeId,
        DateOnly checkIn,
        DateOnly checkOut,
        int roomsRequested,
        CancellationToken cancellationToken = default)
    {
        var nightCount = checkOut.DayNumber - checkIn.DayNumber;
        if (nightCount <= 0)
        {
            return new InventoryAvailabilityResult
            {
                IsAvailable = false,
                MinimumAvailableRooms = 0
            };
        }

        // Load tracked entities (not a scalar projection) so an uncommitted Release
        // in this DbContext is visible. Otherwise modify treats the current booking's
        // own rooms as still occupied.
        var inventories = await _dbContext.RoomInventories
            .Where(i => i.RoomTypeId == roomTypeId
                        && i.Date >= checkIn
                        && i.Date < checkOut)
            .ToListAsync(cancellationToken);

        if (inventories.Count != nightCount)
        {
            return new InventoryAvailabilityResult
            {
                IsAvailable = false,
                MinimumAvailableRooms = inventories.Count == 0 ? 0 : inventories.Min(i => i.AvailableRooms)
            };
        }

        var minimum = inventories.Min(i => i.AvailableRooms);
        return new InventoryAvailabilityResult
        {
            IsAvailable = minimum >= roomsRequested,
            MinimumAvailableRooms = minimum
        };
    }

    public async Task<bool> ReserveAsync(
        Guid roomTypeId,
        DateOnly checkIn,
        DateOnly checkOut,
        int roomsRequested,
        CancellationToken cancellationToken = default)
    {
        var nightCount = checkOut.DayNumber - checkIn.DayNumber;
        if (nightCount <= 0 || roomsRequested <= 0)
        {
            return false;
        }

        var inventories = await _dbContext.RoomInventories
            .Where(i => i.RoomTypeId == roomTypeId
                        && i.Date >= checkIn
                        && i.Date < checkOut)
            .ToListAsync(cancellationToken);

        if (inventories.Count != nightCount
            || inventories.Any(i => i.AvailableRooms < roomsRequested))
        {
            return false;
        }

        foreach (var inventory in inventories)
        {
            inventory.AvailableRooms -= roomsRequested;
        }

        return true;
    }

    public async Task ReleaseAsync(
        Guid roomTypeId,
        DateOnly checkIn,
        DateOnly checkOut,
        int roomsRequested,
        CancellationToken cancellationToken = default)
    {
        if (roomsRequested <= 0)
        {
            return;
        }

        var inventories = await _dbContext.RoomInventories
            .Where(i => i.RoomTypeId == roomTypeId
                        && i.Date >= checkIn
                        && i.Date < checkOut)
            .ToListAsync(cancellationToken);

        foreach (var inventory in inventories)
        {
            inventory.AvailableRooms = Math.Min(
                inventory.TotalRooms,
                inventory.AvailableRooms + roomsRequested);
        }
    }
}
