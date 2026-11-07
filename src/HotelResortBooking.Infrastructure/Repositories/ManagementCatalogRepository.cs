using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Paging;
using HotelResortBooking.Domain.Entities;
using HotelResortBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelResortBooking.Infrastructure.Repositories;

public sealed class ManagementCatalogRepository : IManagementCatalogRepository
{
    private readonly ApplicationDbContext _db;

    public ManagementCatalogRepository(ApplicationDbContext db) => _db = db;

    public async Task<PagedResult<ManagedHotelListItem>> SearchHotelsAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Properties.AsNoTracking().AsQueryable();
        if (isActive is bool active)
        {
            query = query.Where(p => p.IsActive == active);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term)
                || p.City.ToLower().Contains(term)
                || p.Country.ToLower().Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ManagedHotelListItem
            {
                Id = p.Id,
                Name = p.Name,
                City = p.City,
                Country = p.Country,
                PropertyType = p.PropertyType,
                StarRating = p.StarRating,
                IsActive = p.IsActive
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ManagedHotelListItem>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<ManagedHotelDetails?> GetHotelAsync(Guid hotelId, CancellationToken cancellationToken = default)
    {
        var property = await _db.Properties
            .AsNoTracking()
            .Include(p => p.Images)
            .Include(p => p.PropertyAmenities)
            .ThenInclude(pa => pa.Amenity)
            .FirstOrDefaultAsync(p => p.Id == hotelId, cancellationToken);

        return property is null ? null : MapHotel(property);
    }

    public async Task<ManagedHotelDetails> CreateHotelAsync(SaveHotelModel model, CancellationToken cancellationToken = default)
    {
        var property = Apply(new Property(), model);
        _db.Properties.Add(property);
        await Task.CompletedTask;
        return MapHotel(property);
    }

    public async Task<ManagedHotelDetails?> UpdateHotelAsync(
        Guid hotelId,
        SaveHotelModel model,
        CancellationToken cancellationToken = default)
    {
        var property = await _db.Properties
            .Include(p => p.Images)
            .Include(p => p.PropertyAmenities)
            .ThenInclude(pa => pa.Amenity)
            .FirstOrDefaultAsync(p => p.Id == hotelId, cancellationToken);

        if (property is null)
        {
            return null;
        }

        Apply(property, model);
        return MapHotel(property);
    }

    public async Task<bool> SetHotelActiveAsync(Guid hotelId, bool isActive, CancellationToken cancellationToken = default)
    {
        var property = await _db.Properties.FirstOrDefaultAsync(p => p.Id == hotelId, cancellationToken);
        if (property is null)
        {
            return false;
        }

        property.IsActive = isActive;
        return true;
    }

    public async Task<ManagedImageDto> AddHotelImageAsync(
        Guid hotelId,
        string imageUrl,
        string? altText,
        int? sortOrder,
        bool isPrimary,
        CancellationToken cancellationToken = default)
    {
        if (isPrimary)
        {
            await ClearPrimaryAsync(hotelId, cancellationToken);
        }

        var maxOrder = await _db.PropertyImages
            .Where(i => i.PropertyId == hotelId)
            .Select(i => (int?)i.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var image = new PropertyImage
        {
            PropertyId = hotelId,
            ImageUrl = imageUrl,
            AltText = altText,
            SortOrder = sortOrder ?? maxOrder + 1,
            IsPrimary = isPrimary,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.PropertyImages.Add(image);
        return MapImage(image);
    }

    public async Task<ManagedImageDto?> UpdateHotelImageAsync(
        Guid hotelId,
        Guid imageId,
        string? imageUrl,
        string? altText,
        int? sortOrder,
        bool? isPrimary,
        CancellationToken cancellationToken = default)
    {
        var image = await _db.PropertyImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.PropertyId == hotelId, cancellationToken);
        if (image is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            image.ImageUrl = imageUrl;
        }

        if (altText is not null)
        {
            image.AltText = altText;
        }

        if (sortOrder is int order)
        {
            image.SortOrder = order;
        }

        if (isPrimary == true)
        {
            await ClearPrimaryAsync(hotelId, cancellationToken);
            image.IsPrimary = true;
        }

        return MapImage(image);
    }

    public async Task<string?> DeleteHotelImageAsync(Guid hotelId, Guid imageId, CancellationToken cancellationToken = default)
    {
        var image = await _db.PropertyImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.PropertyId == hotelId, cancellationToken);
        if (image is null)
        {
            return null;
        }

        var storedPath = image.ImageUrl;
        _db.PropertyImages.Remove(image);
        return storedPath;
    }

    public async Task<bool> SetPrimaryHotelImageAsync(Guid hotelId, Guid imageId, CancellationToken cancellationToken = default)
    {
        var image = await _db.PropertyImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.PropertyId == hotelId, cancellationToken);
        if (image is null)
        {
            return false;
        }

        await ClearPrimaryAsync(hotelId, cancellationToken);
        image.IsPrimary = true;
        return true;
    }

    public async Task ReorderHotelImagesAsync(
        Guid hotelId,
        IReadOnlyList<Guid> imageIdsInOrder,
        CancellationToken cancellationToken = default)
    {
        var images = await _db.PropertyImages.Where(i => i.PropertyId == hotelId).ToListAsync(cancellationToken);
        for (var i = 0; i < imageIdsInOrder.Count; i++)
        {
            var image = images.FirstOrDefault(x => x.Id == imageIdsInOrder[i]);
            if (image is not null)
            {
                image.SortOrder = i;
            }
        }
    }

    public async Task<IReadOnlyList<ManagedAmenityDto>> GetAmenitiesAsync(CancellationToken cancellationToken = default)
    {
        return await _db.Amenities.AsNoTracking()
            .OrderBy(a => a.Name)
            .Select(a => new ManagedAmenityDto
            {
                Id = a.Id,
                Name = a.Name,
                Description = a.Description,
                Icon = a.Icon,
                IsActive = a.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ManagedAmenityDto> CreateAmenityAsync(
        string name,
        string? description,
        string? icon,
        CancellationToken cancellationToken = default)
    {
        var amenity = new Amenity
        {
            Name = name,
            Description = description,
            Icon = icon,
            IsActive = true
        };
        _db.Amenities.Add(amenity);
        await Task.CompletedTask;
        return MapAmenity(amenity);
    }

    public async Task<ManagedAmenityDto?> UpdateAmenityAsync(
        Guid id,
        string name,
        string? description,
        string? icon,
        CancellationToken cancellationToken = default)
    {
        var amenity = await _db.Amenities.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (amenity is null)
        {
            return null;
        }

        amenity.Name = name;
        amenity.Description = description;
        amenity.Icon = icon;
        return MapAmenity(amenity);
    }

    public async Task<bool> SetAmenityActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var amenity = await _db.Amenities.FirstOrDefaultAsync(a => a.Id == id, cancellationToken);
        if (amenity is null)
        {
            return false;
        }

        amenity.IsActive = isActive;
        return true;
    }

    public async Task AssignAmenityToPropertyAsync(Guid propertyId, Guid amenityId, CancellationToken cancellationToken = default)
    {
        var exists = await _db.PropertyAmenities
            .AnyAsync(pa => pa.PropertyId == propertyId && pa.AmenityId == amenityId, cancellationToken);
        if (!exists)
        {
            _db.PropertyAmenities.Add(new PropertyAmenity { PropertyId = propertyId, AmenityId = amenityId });
        }
    }

    public async Task RemoveAmenityFromPropertyAsync(Guid propertyId, Guid amenityId, CancellationToken cancellationToken = default)
    {
        var link = await _db.PropertyAmenities
            .FirstOrDefaultAsync(pa => pa.PropertyId == propertyId && pa.AmenityId == amenityId, cancellationToken);
        if (link is not null)
        {
            _db.PropertyAmenities.Remove(link);
        }
    }

    public async Task<PagedResult<ManagedRoomDto>> SearchRoomsAsync(
        Guid? propertyId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _db.RoomTypes.AsNoTracking().Include(rt => rt.Property).Include(rt => rt.Images).AsQueryable();
        if (propertyId is Guid pid)
        {
            query = query.Where(rt => rt.PropertyId == pid);
        }

        if (isActive is bool active)
        {
            query = query.Where(rt => rt.IsActive == active);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(rt => rt.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ManagedRoomDto>
        {
            Items = items.Select(MapRoom).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<ManagedRoomDto?> GetRoomAsync(Guid roomTypeId, CancellationToken cancellationToken = default)
    {
        var room = await _db.RoomTypes
            .Include(rt => rt.Property)
            .Include(rt => rt.Images)
            .FirstOrDefaultAsync(rt => rt.Id == roomTypeId, cancellationToken);
        return room is null ? null : MapRoom(room);
    }

    public async Task<ManagedRoomDto> CreateRoomAsync(SaveRoomTypeModel model, CancellationToken cancellationToken = default)
    {
        var room = ApplyRoom(new RoomType(), model);
        _db.RoomTypes.Add(room);
        room.Property = await _db.Properties.FirstAsync(p => p.Id == model.PropertyId, cancellationToken);
        await Task.CompletedTask;
        return MapRoom(room);
    }

    public async Task<ManagedRoomDto?> UpdateRoomAsync(
        Guid roomTypeId,
        SaveRoomTypeModel model,
        CancellationToken cancellationToken = default)
    {
        var room = await _db.RoomTypes.Include(rt => rt.Property).Include(rt => rt.Images)
            .FirstOrDefaultAsync(rt => rt.Id == roomTypeId, cancellationToken);
        if (room is null)
        {
            return null;
        }

        ApplyRoom(room, model);
        return MapRoom(room);
    }

    public async Task<bool> SetRoomActiveAsync(Guid roomTypeId, bool isActive, CancellationToken cancellationToken = default)
    {
        var room = await _db.RoomTypes.FirstOrDefaultAsync(rt => rt.Id == roomTypeId, cancellationToken);
        if (room is null)
        {
            return false;
        }

        room.IsActive = isActive;
        return true;
    }

    public async Task<ManagedImageDto> AddRoomImageAsync(
        Guid roomTypeId,
        string imageUrl,
        string? altText,
        int? sortOrder,
        bool isPrimary,
        CancellationToken cancellationToken = default)
    {
        if (isPrimary)
        {
            await ClearPrimaryRoomImagesAsync(roomTypeId, cancellationToken);
        }

        var maxOrder = await _db.RoomImages
            .Where(i => i.RoomTypeId == roomTypeId)
            .Select(i => (int?)i.SortOrder)
            .MaxAsync(cancellationToken) ?? -1;

        var image = new RoomImage
        {
            RoomTypeId = roomTypeId,
            ImageUrl = imageUrl,
            AltText = altText,
            SortOrder = sortOrder ?? maxOrder + 1,
            IsPrimary = isPrimary,
            CreatedAt = DateTimeOffset.UtcNow
        };
        _db.RoomImages.Add(image);
        return MapImage(image);
    }

    public async Task<ManagedImageDto?> UpdateRoomImageAsync(
        Guid roomTypeId,
        Guid imageId,
        string? imageUrl,
        string? altText,
        int? sortOrder,
        bool? isPrimary,
        CancellationToken cancellationToken = default)
    {
        var image = await _db.RoomImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.RoomTypeId == roomTypeId, cancellationToken);
        if (image is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            image.ImageUrl = imageUrl;
        }

        if (altText is not null)
        {
            image.AltText = altText;
        }

        if (sortOrder is int order)
        {
            image.SortOrder = order;
        }

        if (isPrimary == true)
        {
            await ClearPrimaryRoomImagesAsync(roomTypeId, cancellationToken);
            image.IsPrimary = true;
        }

        return MapImage(image);
    }

    public async Task<string?> DeleteRoomImageAsync(Guid roomTypeId, Guid imageId, CancellationToken cancellationToken = default)
    {
        var image = await _db.RoomImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.RoomTypeId == roomTypeId, cancellationToken);
        if (image is null)
        {
            return null;
        }

        var storedPath = image.ImageUrl;
        _db.RoomImages.Remove(image);
        return storedPath;
    }

    public async Task<bool> SetPrimaryRoomImageAsync(Guid roomTypeId, Guid imageId, CancellationToken cancellationToken = default)
    {
        var image = await _db.RoomImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.RoomTypeId == roomTypeId, cancellationToken);
        if (image is null)
        {
            return false;
        }

        await ClearPrimaryRoomImagesAsync(roomTypeId, cancellationToken);
        image.IsPrimary = true;
        return true;
    }

    public async Task ReorderRoomImagesAsync(
        Guid roomTypeId,
        IReadOnlyList<Guid> imageIdsInOrder,
        CancellationToken cancellationToken = default)
    {
        var images = await _db.RoomImages.Where(i => i.RoomTypeId == roomTypeId).ToListAsync(cancellationToken);
        for (var i = 0; i < imageIdsInOrder.Count; i++)
        {
            var image = images.FirstOrDefault(x => x.Id == imageIdsInOrder[i]);
            if (image is not null)
            {
                image.SortOrder = i;
            }
        }
    }

    public async Task<IReadOnlyList<RoomInventoryDto>> GetInventoryAsync(
        Guid roomTypeId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        return await _db.RoomInventories.AsNoTracking()
            .Where(i => i.RoomTypeId == roomTypeId && i.Date >= from && i.Date <= to)
            .OrderBy(i => i.Date)
            .Select(i => new RoomInventoryDto
            {
                Id = i.Id,
                Date = i.Date,
                TotalRooms = i.TotalRooms,
                AvailableRooms = i.AvailableRooms
            })
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertInventoryAsync(
        Guid roomTypeId,
        IReadOnlyList<RoomInventoryUpdateItem> items,
        CancellationToken cancellationToken = default)
    {
        var dates = items.Select(i => i.Date).ToList();
        var existing = await _db.RoomInventories
            .Where(i => i.RoomTypeId == roomTypeId && dates.Contains(i.Date))
            .ToListAsync(cancellationToken);

        foreach (var item in items)
        {
            var row = existing.FirstOrDefault(e => e.Date == item.Date);
            if (row is null)
            {
                _db.RoomInventories.Add(new RoomInventory
                {
                    RoomTypeId = roomTypeId,
                    Date = item.Date,
                    TotalRooms = item.TotalRooms,
                    AvailableRooms = item.AvailableRooms
                });
            }
            else
            {
                row.TotalRooms = item.TotalRooms;
                row.AvailableRooms = item.AvailableRooms;
            }
        }
    }

    public async Task<IReadOnlyList<ManagedUpgradeDto>> GetUpgradesAsync(
        Guid? propertyId,
        CancellationToken cancellationToken = default)
    {
        var query = _db.UpgradeOptions.AsNoTracking().Include(u => u.RoomTypeUpgradeOptions).AsQueryable();
        if (propertyId is Guid pid)
        {
            query = query.Where(u => u.PropertyId == pid);
        }

        var items = await query.OrderBy(u => u.Name).ToListAsync(cancellationToken);
        return items.Select(MapUpgrade).ToList();
    }

    public async Task<ManagedUpgradeDto?> GetUpgradeAsync(Guid upgradeId, CancellationToken cancellationToken = default)
    {
        var upgrade = await _db.UpgradeOptions
            .Include(u => u.RoomTypeUpgradeOptions)
            .FirstOrDefaultAsync(u => u.Id == upgradeId, cancellationToken);
        return upgrade is null ? null : MapUpgrade(upgrade);
    }

    public async Task<ManagedUpgradeDto> CreateUpgradeAsync(SaveUpgradeModel model, CancellationToken cancellationToken = default)
    {
        var upgrade = ApplyUpgrade(new UpgradeOption(), model);
        foreach (var roomTypeId in model.RoomTypeIds.Distinct())
        {
            upgrade.RoomTypeUpgradeOptions.Add(new RoomTypeUpgradeOption { RoomTypeId = roomTypeId });
        }

        _db.UpgradeOptions.Add(upgrade);
        await Task.CompletedTask;
        return MapUpgrade(upgrade);
    }

    public async Task<ManagedUpgradeDto?> UpdateUpgradeAsync(
        Guid upgradeId,
        SaveUpgradeModel model,
        CancellationToken cancellationToken = default)
    {
        var upgrade = await _db.UpgradeOptions
            .Include(u => u.RoomTypeUpgradeOptions)
            .FirstOrDefaultAsync(u => u.Id == upgradeId, cancellationToken);
        if (upgrade is null)
        {
            return null;
        }

        ApplyUpgrade(upgrade, model);
        _db.RoomTypeUpgradeOptions.RemoveRange(upgrade.RoomTypeUpgradeOptions);
        foreach (var roomTypeId in model.RoomTypeIds.Distinct())
        {
            upgrade.RoomTypeUpgradeOptions.Add(new RoomTypeUpgradeOption
            {
                UpgradeOptionId = upgrade.Id,
                RoomTypeId = roomTypeId
            });
        }

        return MapUpgrade(upgrade);
    }

    public async Task<bool> DeleteUpgradeAsync(Guid upgradeId, CancellationToken cancellationToken = default)
    {
        var upgrade = await _db.UpgradeOptions.FirstOrDefaultAsync(u => u.Id == upgradeId, cancellationToken);
        if (upgrade is null)
        {
            return false;
        }

        upgrade.IsActive = false;
        return true;
    }

    private async Task ClearPrimaryAsync(Guid hotelId, CancellationToken cancellationToken)
    {
        var images = await _db.PropertyImages.Where(i => i.PropertyId == hotelId && i.IsPrimary).ToListAsync(cancellationToken);
        foreach (var image in images)
        {
            image.IsPrimary = false;
        }
    }

    private async Task ClearPrimaryRoomImagesAsync(Guid roomTypeId, CancellationToken cancellationToken)
    {
        var images = await _db.RoomImages.Where(i => i.RoomTypeId == roomTypeId && i.IsPrimary).ToListAsync(cancellationToken);
        foreach (var image in images)
        {
            image.IsPrimary = false;
        }
    }

    private static Property Apply(Property property, SaveHotelModel model)
    {
        property.Name = model.Name;
        property.Description = model.Description;
        property.PropertyType = model.PropertyType;
        property.StarRating = model.StarRating;
        property.AddressLine = model.AddressLine;
        property.City = model.City;
        property.Country = model.Country;
        property.Latitude = model.Latitude;
        property.Longitude = model.Longitude;
        property.CheckInTime = model.CheckInTime;
        property.CheckOutTime = model.CheckOutTime;
        property.CancellationPolicy = model.CancellationPolicy;
        property.DepositPolicy = model.DepositPolicy;
        property.IsActive = model.IsActive;
        return property;
    }

    private static RoomType ApplyRoom(RoomType room, SaveRoomTypeModel model)
    {
        room.PropertyId = model.PropertyId;
        room.Name = model.Name;
        room.Description = model.Description;
        room.RoomSize = model.RoomSize;
        room.MaxAdults = model.MaxAdults;
        room.MaxChildren = model.MaxChildren;
        room.MaxGuests = model.MaxGuests;
        room.BedType = model.BedType;
        room.BasePricePerNight = model.BasePricePerNight;
        room.Currency = model.Currency;
        room.IsActive = model.IsActive;
        return room;
    }

    private static UpgradeOption ApplyUpgrade(UpgradeOption upgrade, SaveUpgradeModel model)
    {
        upgrade.PropertyId = model.PropertyId;
        upgrade.Name = model.Name;
        upgrade.Description = model.Description;
        upgrade.UpgradeType = model.UpgradeType;
        upgrade.Price = model.Price;
        upgrade.Currency = model.Currency;
        upgrade.IsMandatory = model.IsMandatory;
        upgrade.IsRecommended = model.IsRecommended;
        upgrade.IsActive = model.IsActive;
        return upgrade;
    }

    private static ManagedHotelDetails MapHotel(Property property) => new()
    {
        Id = property.Id,
        Name = property.Name,
        Description = property.Description,
        PropertyType = property.PropertyType,
        StarRating = property.StarRating,
        AddressLine = property.AddressLine,
        City = property.City,
        Country = property.Country,
        Latitude = property.Latitude,
        Longitude = property.Longitude,
        CheckInTime = property.CheckInTime,
        CheckOutTime = property.CheckOutTime,
        CancellationPolicy = property.CancellationPolicy,
        DepositPolicy = property.DepositPolicy,
        IsActive = property.IsActive,
        Images = property.Images.OrderBy(i => i.SortOrder).Select(MapImage).ToList(),
        Amenities = property.PropertyAmenities.Select(pa => MapAmenity(pa.Amenity)).ToList()
    };

    private static ManagedImageDto MapImage(PropertyImage image) => new()
    {
        Id = image.Id,
        ImageUrl = image.ImageUrl,
        AltText = image.AltText,
        SortOrder = image.SortOrder,
        IsPrimary = image.IsPrimary
    };

    private static ManagedImageDto MapImage(RoomImage image) => new()
    {
        Id = image.Id,
        ImageUrl = image.ImageUrl,
        AltText = image.AltText,
        SortOrder = image.SortOrder,
        IsPrimary = image.IsPrimary
    };

    private static ManagedAmenityDto MapAmenity(Amenity amenity) => new()
    {
        Id = amenity.Id,
        Name = amenity.Name,
        Description = amenity.Description,
        Icon = amenity.Icon,
        IsActive = amenity.IsActive
    };

    private static ManagedRoomDto MapRoom(RoomType room) => new()
    {
        Id = room.Id,
        PropertyId = room.PropertyId,
        PropertyName = room.Property?.Name ?? string.Empty,
        Name = room.Name,
        Description = room.Description,
        RoomSize = room.RoomSize,
        MaxAdults = room.MaxAdults,
        MaxChildren = room.MaxChildren,
        MaxGuests = room.MaxGuests,
        BedType = room.BedType,
        BasePricePerNight = room.BasePricePerNight,
        Currency = room.Currency,
        IsActive = room.IsActive,
        Images = room.Images.OrderBy(i => i.SortOrder).Select(MapImage).ToList()
    };

    private static ManagedUpgradeDto MapUpgrade(UpgradeOption upgrade) => new()
    {
        Id = upgrade.Id,
        PropertyId = upgrade.PropertyId,
        Name = upgrade.Name,
        Description = upgrade.Description,
        UpgradeType = upgrade.UpgradeType,
        Price = upgrade.Price,
        Currency = upgrade.Currency,
        IsMandatory = upgrade.IsMandatory,
        IsRecommended = upgrade.IsRecommended,
        IsActive = upgrade.IsActive,
        RoomTypeIds = upgrade.RoomTypeUpgradeOptions.Select(x => x.RoomTypeId).ToList()
    };
}
