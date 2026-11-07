using HotelResortBooking.Application.Abstractions.Repositories;

namespace HotelResortBooking.UnitTests.Fakes;

public sealed class FakePropertyRepository : IPropertyRepository
{
    public List<PropertyDetailsData> Details { get; } = [];
    public List<PropertySearchItem> SearchItems { get; } = [];
    public HashSet<Guid> ActivePropertyIds { get; } = [];
    public HashSet<string> KnownDestinations { get; } = new(StringComparer.OrdinalIgnoreCase);

    public void Seed(PropertyDetailsData details, bool isActive = true, PropertySearchItem? searchItem = null)
    {
        Details.Add(details);
        if (isActive)
        {
            ActivePropertyIds.Add(details.Id);
        }

        if (searchItem is not null)
        {
            SearchItems.Add(searchItem);
        }
        else
        {
            SearchItems.Add(new PropertySearchItem
            {
                PropertyId = details.Id,
                PropertyName = details.Name,
                StarRating = details.StarRating,
                ReviewScore = details.ReviewScore,
                ReviewCount = details.ReviewCount,
                PropertyType = details.PropertyType,
                City = details.City,
                Country = details.Country,
                PricePerNight = details.Rooms.FirstOrDefault()?.BasePricePerNight ?? 100m,
                TotalPrice = details.Rooms.FirstOrDefault()?.BasePricePerNight ?? 100m,
                Currency = details.Rooms.FirstOrDefault()?.Currency ?? "USD",
                AvailableRoomCount = 1,
                Latitude = details.Latitude,
                Longitude = details.Longitude,
                CancellationPolicy = details.CancellationPolicy
            });
        }
    }

    public Task<bool> DestinationExistsAsync(string destination, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            return Task.FromResult(true);
        }

        var term = destination.Trim();
        if (KnownDestinations.Contains(term))
        {
            return Task.FromResult(true);
        }

        var exists = Details.Any(p =>
                Contains(p.City, term) || Contains(p.Country, term) || Contains(p.Name, term))
            || SearchItems.Any(p =>
                Contains(p.City, term) || Contains(p.Country, term) || Contains(p.PropertyName, term));

        return Task.FromResult(exists);
    }

    public Task<PropertySearchPage> SearchAsync(
        PropertySearchCriteria criteria,
        CancellationToken cancellationToken = default)
    {
        IEnumerable<PropertySearchItem> query = SearchItems.Where(i => ActivePropertyIds.Contains(i.PropertyId));

        if (!string.IsNullOrWhiteSpace(criteria.Destination))
        {
            var term = criteria.Destination.Trim();
            query = query.Where(i =>
                Contains(i.City, term) || Contains(i.Country, term) || Contains(i.PropertyName, term));
        }

        if (criteria.PropertyType is not null)
        {
            query = query.Where(i => i.PropertyType == criteria.PropertyType);
        }

        if (criteria.MinPrice is not null)
        {
            query = query.Where(i => i.PricePerNight >= criteria.MinPrice);
        }

        if (criteria.MaxPrice is not null)
        {
            query = query.Where(i => i.PricePerNight <= criteria.MaxPrice);
        }

        if (criteria.MinimumStarRating is not null)
        {
            query = query.Where(i => i.StarRating >= criteria.MinimumStarRating);
        }

        if (criteria.MinimumReviewScore is not null)
        {
            query = query.Where(i => i.ReviewScore >= criteria.MinimumReviewScore);
        }

        var all = query.ToList();
        var page = Math.Max(1, criteria.Page);
        var pageSize = Math.Max(1, criteria.PageSize);
        var items = all.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        return Task.FromResult(new PropertySearchPage
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = all.Count
        });
    }

    public Task<PropertyDetailsData?> GetDetailsAsync(
        Guid propertyId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Details.FirstOrDefault(d => d.Id == propertyId));

    public Task<bool> ExistsActiveAsync(Guid propertyId, CancellationToken cancellationToken = default)
        => Task.FromResult(ActivePropertyIds.Contains(propertyId));

    public Task<PropertySearchPage> SearchByLocationAsync(
        PropertySearchCriteria criteria,
        CancellationToken cancellationToken = default)
        => SearchAsync(criteria, cancellationToken);

    private static bool Contains(string? value, string term)
        => !string.IsNullOrWhiteSpace(value)
           && value.Contains(term, StringComparison.OrdinalIgnoreCase);
}
