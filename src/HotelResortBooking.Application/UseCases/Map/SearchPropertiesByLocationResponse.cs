namespace HotelResortBooking.Application.UseCases.Map;

public sealed class SearchPropertiesByLocationResponse
{
    public IReadOnlyList<MapPropertyPinDto> Pins { get; init; } = Array.Empty<MapPropertyPinDto>();

    public int Page { get; init; }

    public int PageSize { get; init; }

    public int TotalCount { get; init; }

    public int TotalPages { get; init; }
}

public sealed class MapPropertyPinDto
{
    public Guid PropertyId { get; init; }

    public string PropertyName { get; init; } = string.Empty;

    public string? MainImageUrl { get; init; }

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public decimal PricePerNight { get; init; }

    public decimal TotalPrice { get; init; }

    public string Currency { get; init; } = string.Empty;

    public decimal ReviewScore { get; init; }

    public int StarRating { get; init; }

    public string City { get; init; } = string.Empty;
}
