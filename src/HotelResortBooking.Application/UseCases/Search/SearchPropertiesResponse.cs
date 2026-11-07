using HotelResortBooking.Application.Abstractions.Repositories;

namespace HotelResortBooking.Application.UseCases.Search;

public sealed class SearchPropertiesResponse
{
    public IReadOnlyList<PropertySearchItem> Items { get; init; } = Array.Empty<PropertySearchItem>();
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages { get; init; }
}
