using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Domain.Entities;
using HotelResortBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelResortBooking.Infrastructure.Repositories;

public sealed class PropertyRepository : IPropertyRepository
{
    private const double EarthRadiusKm = 6371.0;

    private readonly ApplicationDbContext _dbContext;
    private readonly IImageUrlResolver _urls;

    public PropertyRepository(ApplicationDbContext dbContext, IImageUrlResolver urls)
    {
        _dbContext = dbContext;
        _urls = urls;
    }

    public async Task<bool> DestinationExistsAsync(
        string destination,
        CancellationToken cancellationToken = default)
    {
        var term = $"%{destination.Trim()}%";

        return await _dbContext.Properties.AsNoTracking()
            .AnyAsync(
                p => p.IsActive
                     && (EF.Functions.ILike(p.City, term)
                         || EF.Functions.ILike(p.Country, term)
                         || EF.Functions.ILike(p.Name, term)),
                cancellationToken);
    }

    public Task<PropertySearchPage> SearchAsync(
        PropertySearchCriteria criteria,
        CancellationToken cancellationToken = default)
        => SearchCoreAsync(criteria, applyLocationFilter: false, cancellationToken);

    public Task<PropertySearchPage> SearchByLocationAsync(
        PropertySearchCriteria criteria,
        CancellationToken cancellationToken = default)
        => SearchCoreAsync(criteria, applyLocationFilter: true, cancellationToken);

    public async Task<PropertyDetailsData?> GetDetailsAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default)
    {
        var property = await _dbContext.Properties
            .AsNoTracking()
            .Include(p => p.Images)
            .Include(p => p.PropertyAmenities)
            .ThenInclude(pa => pa.Amenity)
            .Include(p => p.RoomTypes.Where(rt => rt.IsActive))
            .Include(p => p.UpgradeOptions.Where(u => u.IsActive))
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.IsActive, cancellationToken);

        if (property is null)
        {
            return null;
        }

        return new PropertyDetailsData
        {
            Id = property.Id,
            Name = property.Name,
            Description = property.Description,
            PropertyType = property.PropertyType,
            StarRating = property.StarRating,
            ReviewScore = property.ReviewScore,
            ReviewCount = property.ReviewCount,
            AddressLine = property.AddressLine,
            City = property.City,
            Country = property.Country,
            PostalCode = property.PostalCode,
            Latitude = property.Latitude,
            Longitude = property.Longitude,
            CheckInTime = property.CheckInTime,
            CheckOutTime = property.CheckOutTime,
            CancellationPolicy = property.CancellationPolicy,
            DepositPolicy = property.DepositPolicy,
            Images = property.Images
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
            Amenities = property.PropertyAmenities
                .Where(pa => pa.Amenity.IsActive)
                .Select(pa => pa.Amenity)
                .OrderBy(a => a.Name)
                .Select(a => new AmenityData
                {
                    Id = a.Id,
                    Name = a.Name,
                    Description = a.Description,
                    Icon = a.Icon
                })
                .ToList(),
            Rooms = property.RoomTypes
                .OrderBy(rt => rt.BasePricePerNight)
                .Select(rt => new RoomTypeSummaryData
                {
                    Id = rt.Id,
                    Name = rt.Name,
                    Description = rt.Description,
                    MaxAdults = rt.MaxAdults,
                    MaxChildren = rt.MaxChildren,
                    MaxGuests = rt.MaxGuests,
                    BedType = rt.BedType,
                    BasePricePerNight = rt.BasePricePerNight,
                    Currency = rt.Currency
                })
                .ToList(),
            UpgradeOptions = property.UpgradeOptions
                .OrderByDescending(u => u.IsRecommended)
                .ThenBy(u => u.Name)
                .Select(UpgradeRepository.Map)
                .ToList()
        };
    }

    public Task<bool> ExistsActiveAsync(Guid propertyId, CancellationToken cancellationToken = default)
    {
        return _dbContext.Properties.AsNoTracking()
            .AnyAsync(p => p.Id == propertyId && p.IsActive, cancellationToken);
    }

    private async Task<PropertySearchPage> SearchCoreAsync(
        PropertySearchCriteria criteria,
        bool applyLocationFilter,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, criteria.Page);
        var pageSize = Math.Max(1, criteria.PageSize);
        var nights = criteria.CheckOutDate.DayNumber - criteria.CheckInDate.DayNumber;
        if (nights <= 0)
        {
            return EmptyPage(page, pageSize);
        }

        var roomsNeeded = Math.Max(1, criteria.Rooms);
        var checkIn = criteria.CheckInDate;
        var checkOut = criteria.CheckOutDate;

        var query = _dbContext.Properties.AsNoTracking().Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(criteria.Destination))
        {
            var term = $"%{criteria.Destination.Trim()}%";
            query = query.Where(p =>
                EF.Functions.ILike(p.City, term)
                || EF.Functions.ILike(p.Country, term)
                || EF.Functions.ILike(p.Name, term));
        }

        if (criteria.PropertyType is not null)
        {
            query = query.Where(p => p.PropertyType == criteria.PropertyType);
        }

        if (criteria.MinimumStarRating is int minStars)
        {
            query = query.Where(p => p.StarRating >= minStars);
        }

        if (criteria.MinimumReviewScore is decimal minScore)
        {
            query = query.Where(p => p.ReviewScore >= minScore);
        }

        if (!string.IsNullOrWhiteSpace(criteria.CancellationPolicyContains))
        {
            var policyTerm = $"%{criteria.CancellationPolicyContains.Trim()}%";
            query = query.Where(p =>
                p.CancellationPolicy != null
                && EF.Functions.ILike(p.CancellationPolicy, policyTerm));
        }

        if (criteria.AmenityIds is { Count: > 0 })
        {
            foreach (var amenityId in criteria.AmenityIds.Distinct())
            {
                var id = amenityId;
                query = query.Where(p => p.PropertyAmenities.Any(pa => pa.AmenityId == id));
            }
        }

        if (applyLocationFilter
            && criteria.Latitude is double latitude
            && criteria.Longitude is double longitude
            && criteria.RadiusKm is double radiusKm
            && radiusKm > 0)
        {
            // Approximate bounding box in SQL, refine with Haversine in memory.
            var latDelta = radiusKm / 111.0;
            var cosLat = Math.Cos(DegreesToRadians(latitude));
            var lonDelta = radiusKm / (111.0 * Math.Max(0.01, Math.Abs(cosLat)));

            query = query.Where(p =>
                p.Latitude >= latitude - latDelta
                && p.Latitude <= latitude + latDelta
                && p.Longitude >= longitude - lonDelta
                && p.Longitude <= longitude + lonDelta);
        }

        // Restrict to properties that have at least one active room type available for the full stay.
        query = query.Where(p => p.RoomTypes.Any(rt =>
            rt.IsActive
            && rt.Inventories.Count(i =>
                i.Date >= checkIn
                && i.Date < checkOut
                && i.AvailableRooms >= roomsNeeded) == nights));

        var candidates = await query
            .Select(p => new PropertyCandidate
            {
                Id = p.Id,
                Name = p.Name,
                StarRating = p.StarRating,
                ReviewScore = p.ReviewScore,
                ReviewCount = p.ReviewCount,
                PropertyType = p.PropertyType,
                City = p.City,
                Country = p.Country,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                CancellationPolicy = p.CancellationPolicy,
                MainImageUrl = p.Images
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.SortOrder)
                    .Select(i => i.ImageUrl)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        if (applyLocationFilter
            && criteria.Latitude is double lat
            && criteria.Longitude is double lon
            && criteria.RadiusKm is double radius
            && radius > 0)
        {
            candidates = candidates
                .Where(p => HaversineKm(lat, lon, p.Latitude, p.Longitude) <= radius)
                .ToList();
        }

        if (candidates.Count == 0)
        {
            return EmptyPage(page, pageSize);
        }

        var propertyIds = candidates.Select(c => c.Id).ToList();

        var roomPricing = await _dbContext.RoomTypes
            .AsNoTracking()
            .Where(rt => propertyIds.Contains(rt.PropertyId) && rt.IsActive)
            .Where(rt => rt.Inventories.Count(i =>
                i.Date >= checkIn
                && i.Date < checkOut
                && i.AvailableRooms >= roomsNeeded) == nights)
            .Select(rt => new
            {
                rt.PropertyId,
                rt.BasePricePerNight,
                rt.Currency,
                MinAvailable = rt.Inventories
                    .Where(i => i.Date >= checkIn && i.Date < checkOut)
                    .Min(i => i.AvailableRooms)
            })
            .ToListAsync(cancellationToken);

        var priceByProperty = roomPricing
            .GroupBy(r => r.PropertyId)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var cheapest = g.OrderBy(x => x.BasePricePerNight).First();
                    return (
                        PricePerNight: cheapest.BasePricePerNight,
                        Currency: cheapest.Currency,
                        AvailableRoomCount: g.Max(x => x.MinAvailable));
                });

        var items = new List<PropertySearchItem>();
        foreach (var candidate in candidates)
        {
            if (!priceByProperty.TryGetValue(candidate.Id, out var pricing))
            {
                continue;
            }

            if (criteria.MinPrice is decimal minPrice && pricing.PricePerNight < minPrice)
            {
                continue;
            }

            if (criteria.MaxPrice is decimal maxPrice && pricing.PricePerNight > maxPrice)
            {
                continue;
            }

            items.Add(new PropertySearchItem
            {
                PropertyId = candidate.Id,
                PropertyName = candidate.Name,
                MainImageUrl = candidate.MainImageUrl is null ? null : _urls.ToPublicUrl(candidate.MainImageUrl),
                StarRating = candidate.StarRating,
                ReviewScore = candidate.ReviewScore,
                ReviewCount = candidate.ReviewCount,
                PropertyType = candidate.PropertyType,
                City = candidate.City,
                Country = candidate.Country,
                PricePerNight = pricing.PricePerNight,
                TotalPrice = pricing.PricePerNight * nights * roomsNeeded,
                Currency = pricing.Currency,
                AvailableRoomCount = pricing.AvailableRoomCount,
                Latitude = candidate.Latitude,
                Longitude = candidate.Longitude,
                CancellationPolicy = candidate.CancellationPolicy
            });
        }

        items = Sort(items, criteria.SortBy).ToList();

        var totalCount = items.Count;
        var pageItems = items
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return new PropertySearchPage
        {
            Items = pageItems,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    private static IEnumerable<PropertySearchItem> Sort(
        IEnumerable<PropertySearchItem> items,
        PropertySortBy sortBy) =>
        sortBy switch
        {
            PropertySortBy.PriceAscending => items.OrderBy(i => i.PricePerNight).ThenByDescending(i => i.ReviewScore),
            PropertySortBy.PriceDescending => items.OrderByDescending(i => i.PricePerNight).ThenByDescending(i => i.ReviewScore),
            PropertySortBy.RatingDescending => items.OrderByDescending(i => i.ReviewScore).ThenBy(i => i.PricePerNight),
            PropertySortBy.StarRatingDescending => items.OrderByDescending(i => i.StarRating).ThenByDescending(i => i.ReviewScore),
            _ => items.OrderByDescending(i => i.ReviewScore).ThenBy(i => i.PricePerNight)
        };

    private static PropertySearchPage EmptyPage(int page, int pageSize) => new()
    {
        Items = Array.Empty<PropertySearchItem>(),
        Page = page,
        PageSize = pageSize,
        TotalCount = 0
    };

    private static double DegreesToRadians(double degrees) => degrees * Math.PI / 180.0;

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(DegreesToRadians(lat1))
                * Math.Cos(DegreesToRadians(lat2))
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return EarthRadiusKm * c;
    }

    private sealed class PropertyCandidate
    {
        public Guid Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public int StarRating { get; init; }

        public decimal ReviewScore { get; init; }

        public int ReviewCount { get; init; }

        public Domain.Enums.PropertyType PropertyType { get; init; }

        public string City { get; init; } = string.Empty;

        public string Country { get; init; } = string.Empty;

        public double Latitude { get; init; }

        public double Longitude { get; init; }

        public string? CancellationPolicy { get; init; }

        public string? MainImageUrl { get; init; }
    }
}
