using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Domain.Entities;
using HotelResortBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelResortBooking.Infrastructure.Repositories;

public sealed class RoomTypeRepository : IRoomTypeRepository
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IImageUrlResolver _urls;

    public RoomTypeRepository(ApplicationDbContext dbContext, IImageUrlResolver urls)
    {
        _dbContext = dbContext;
        _urls = urls;
    }

    public async Task<AvailableRoomData?> GetByIdAsync(
        Guid roomTypeId,
        CancellationToken cancellationToken = default)
    {
        var roomType = await _dbContext.RoomTypes
            .AsNoTracking()
            .Include(rt => rt.Images)
            .Include(rt => rt.RoomTypeUpgradeOptions)
            .ThenInclude(link => link.UpgradeOption)
            .FirstOrDefaultAsync(rt => rt.Id == roomTypeId && rt.IsActive, cancellationToken);

        if (roomType is null)
        {
            return null;
        }

        var propertyUpgrades = await _dbContext.UpgradeOptions
            .AsNoTracking()
            .Include(u => u.RoomTypeUpgradeOptions)
            .Where(u => u.PropertyId == roomType.PropertyId && u.IsActive)
            .Where(u =>
                !u.RoomTypeUpgradeOptions.Any()
                || u.RoomTypeUpgradeOptions.Any(link => link.RoomTypeId == roomType.Id))
            .ToListAsync(cancellationToken);

        return Map(roomType, availableCount: 0, propertyUpgrades);
    }

    public async Task<IReadOnlyList<AvailableRoomData>> GetAvailableRoomsAsync(
        Guid propertyId,
        DateOnly checkIn,
        DateOnly checkOut,
        int roomsRequested,
        CancellationToken cancellationToken = default)
    {
        var nightCount = checkOut.DayNumber - checkIn.DayNumber;
        if (nightCount <= 0)
        {
            return Array.Empty<AvailableRoomData>();
        }

        var roomTypes = await _dbContext.RoomTypes
            .AsNoTracking()
            .Include(rt => rt.Images)
            .Include(rt => rt.Inventories.Where(i => i.Date >= checkIn && i.Date < checkOut))
            .Where(rt => rt.PropertyId == propertyId && rt.IsActive)
            .ToListAsync(cancellationToken);

        var available = roomTypes
            .Where(rt =>
                rt.Inventories.Count == nightCount
                && rt.Inventories.All(i => i.AvailableRooms >= roomsRequested))
            .ToList();

        if (available.Count == 0)
        {
            return Array.Empty<AvailableRoomData>();
        }

        var upgrades = await _dbContext.UpgradeOptions
            .AsNoTracking()
            .Include(u => u.RoomTypeUpgradeOptions)
            .Where(u => u.PropertyId == propertyId && u.IsActive)
            .ToListAsync(cancellationToken);

        return available
            .Select(rt =>
            {
                var roomUpgrades = upgrades
                    .Where(u =>
                        !u.RoomTypeUpgradeOptions.Any()
                        || u.RoomTypeUpgradeOptions.Any(link => link.RoomTypeId == rt.Id))
                    .ToList();

                var minAvailable = rt.Inventories.Min(i => i.AvailableRooms);
                return Map(rt, minAvailable, roomUpgrades);
            })
            .OrderBy(r => r.PricePerNight)
            .ToList();
    }

    private AvailableRoomData Map(
        RoomType roomType,
        int availableCount,
        IReadOnlyList<UpgradeOption> upgrades) => new()
    {
        RoomTypeId = roomType.Id,
        PropertyId = roomType.PropertyId,
        Name = roomType.Name,
        Description = roomType.Description,
        MaxAdults = roomType.MaxAdults,
        MaxChildren = roomType.MaxChildren,
        MaxGuests = roomType.MaxGuests,
        BedType = roomType.BedType,
        PricePerNight = roomType.BasePricePerNight,
        Currency = roomType.Currency,
        AvailableCount = availableCount,
        Images = roomType.Images
            .OrderByDescending(i => i.IsPrimary)
            .ThenBy(i => i.SortOrder)
            .Select(i => new PropertyImageData
            {
                Id = i.Id,
                ImageUrl = _urls.ToPublicUrl(i.ImageUrl),
                AltText = i.AltText,
                SortOrder = i.SortOrder,
                IsPrimary = i.IsPrimary
            })
            .ToList(),
        UpgradeOptions = upgrades
            .OrderByDescending(u => u.IsRecommended)
            .ThenBy(u => u.Name)
            .Select(UpgradeRepository.Map)
            .ToList()
    };
}
