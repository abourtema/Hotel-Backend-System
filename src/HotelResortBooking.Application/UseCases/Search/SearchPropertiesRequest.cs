using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.UseCases.Search;

public sealed class SearchPropertiesRequest
{
    public string? Destination { get; init; }
    public DateOnly CheckInDate { get; init; }
    public DateOnly CheckOutDate { get; init; }
    public int Adults { get; init; } = 1;
    public int Children { get; init; }
    public int Rooms { get; init; } = 1;
    public PropertyType? PropertyType { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }
    public int? MinimumRating { get; init; }
    public decimal? MinimumReviewScore { get; init; }
    public IReadOnlyCollection<Guid>? Amenities { get; init; }
    public string? CancellationPolicy { get; init; }
    public PropertySortBy SortBy { get; init; } = PropertySortBy.Recommended;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
