using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelResortBooking.Infrastructure.Repositories;

public sealed class UpgradeRepository : IUpgradeRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UpgradeRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<UpgradeOptionData?> GetByIdAsync(
        Guid upgradeOptionId,
        CancellationToken cancellationToken = default)
    {
        var upgrade = await _dbContext.UpgradeOptions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.Id == upgradeOptionId && u.IsActive,
                cancellationToken);

        return upgrade is null ? null : Map(upgrade);
    }

    public async Task<IReadOnlyList<UpgradeOptionData>> GetForPropertyAsync(
        Guid propertyId,
        Guid? roomTypeId = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.UpgradeOptions
            .AsNoTracking()
            .Where(u => u.PropertyId == propertyId && u.IsActive);

        if (roomTypeId is Guid roomType)
        {
            query = query.Where(u =>
                !u.RoomTypeUpgradeOptions.Any()
                || u.RoomTypeUpgradeOptions.Any(link => link.RoomTypeId == roomType));
        }

        var upgrades = await query
            .OrderByDescending(u => u.IsRecommended)
            .ThenBy(u => u.Name)
            .ToListAsync(cancellationToken);

        return upgrades.Select(Map).ToList();
    }

    internal static UpgradeOptionData Map(Domain.Entities.UpgradeOption upgrade) => new()
    {
        Id = upgrade.Id,
        Name = upgrade.Name,
        Description = upgrade.Description,
        UpgradeType = upgrade.UpgradeType,
        Price = upgrade.Price,
        Currency = upgrade.Currency,
        IsMandatory = upgrade.IsMandatory,
        IsRecommended = upgrade.IsRecommended
    };
}
