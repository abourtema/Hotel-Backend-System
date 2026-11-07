using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.Abstractions.Repositories;

public sealed class PropertySearchCriteria
{
    public string? Destination { get; init; }

    public DateOnly CheckInDate { get; init; }

    public DateOnly CheckOutDate { get; init; }

    public int Adults { get; init; }

    public int Children { get; init; }

    public int Rooms { get; init; }

    public PropertyType? PropertyType { get; init; }

    public decimal? MinPrice { get; init; }

    public decimal? MaxPrice { get; init; }

    public int? MinimumStarRating { get; init; }

    public decimal? MinimumReviewScore { get; init; }

    public IReadOnlyCollection<Guid>? AmenityIds { get; init; }

    public string? CancellationPolicyContains { get; init; }

    public PropertySortBy SortBy { get; init; } = PropertySortBy.Recommended;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }

    public double? RadiusKm { get; init; }
}

public enum PropertySortBy
{
    Recommended = 0,
    PriceAscending = 1,
    PriceDescending = 2,
    RatingDescending = 3,
    StarRatingDescending = 4
}

public sealed class PropertySearchItem
{
    public Guid PropertyId { get; init; }

    public string PropertyName { get; init; } = string.Empty;

    public string? MainImageUrl { get; init; }

    public int StarRating { get; init; }

    public decimal ReviewScore { get; init; }

    public int ReviewCount { get; init; }

    public PropertyType PropertyType { get; init; }

    public string City { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public decimal PricePerNight { get; init; }

    public decimal TotalPrice { get; init; }

    public string Currency { get; init; } = string.Empty;

    public int AvailableRoomCount { get; init; }

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public string? CancellationPolicy { get; init; }
}

public sealed class PropertySearchPage
{
    public IReadOnlyList<PropertySearchItem> Items { get; init; } = Array.Empty<PropertySearchItem>();

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public sealed class PropertyDetailsData
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public PropertyType PropertyType { get; init; }

    public int StarRating { get; init; }

    public decimal ReviewScore { get; init; }

    public int ReviewCount { get; init; }

    public string AddressLine { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public string? PostalCode { get; init; }

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public TimeOnly CheckInTime { get; init; }

    public TimeOnly CheckOutTime { get; init; }

    public string? CancellationPolicy { get; init; }

    public string? DepositPolicy { get; init; }

    public IReadOnlyList<PropertyImageData> Images { get; init; } = Array.Empty<PropertyImageData>();

    public IReadOnlyList<AmenityData> Amenities { get; init; } = Array.Empty<AmenityData>();

    public IReadOnlyList<RoomTypeSummaryData> Rooms { get; init; } = Array.Empty<RoomTypeSummaryData>();

    public IReadOnlyList<UpgradeOptionData> UpgradeOptions { get; init; } = Array.Empty<UpgradeOptionData>();
}

public sealed class PropertyImageData
{
    public Guid Id { get; init; }

    public string ImageUrl { get; init; } = string.Empty;

    public string? AltText { get; init; }

    public int SortOrder { get; init; }

    public bool IsPrimary { get; init; }
}

public sealed class AmenityData
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? Icon { get; init; }
}

public sealed class RoomTypeSummaryData
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int MaxAdults { get; init; }

    public int MaxChildren { get; init; }

    public int MaxGuests { get; init; }

    public string? BedType { get; init; }

    public decimal BasePricePerNight { get; init; }

    public string Currency { get; init; } = string.Empty;
}

public sealed class UpgradeOptionData
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public UpgradeType UpgradeType { get; init; }

    public decimal Price { get; init; }

    public string Currency { get; init; } = string.Empty;

    public bool IsMandatory { get; init; }

    public bool IsRecommended { get; init; }
}

public interface IPropertyRepository
{
    Task<bool> DestinationExistsAsync(string destination, CancellationToken cancellationToken = default);

    Task<PropertySearchPage> SearchAsync(PropertySearchCriteria criteria, CancellationToken cancellationToken = default);

    Task<PropertyDetailsData?> GetDetailsAsync(Guid propertyId, CancellationToken cancellationToken = default);

    Task<bool> ExistsActiveAsync(Guid propertyId, CancellationToken cancellationToken = default);

    Task<PropertySearchPage> SearchByLocationAsync(PropertySearchCriteria criteria, CancellationToken cancellationToken = default);
}
