using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.UseCases.Search;
using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Api.Contracts.Hotels;

public sealed class HotelSearchQueryParameters
{
    public string? Destination { get; init; }

    public DateOnly? CheckInDate { get; init; }

    public DateOnly? CheckOutDate { get; init; }

    public int Adults { get; init; } = 1;

    public int Children { get; init; }

    public int Rooms { get; init; } = 1;

    public PropertyType? PropertyType { get; init; }

    public decimal? MinPrice { get; init; }

    public decimal? MaxPrice { get; init; }

    public int? MinimumRating { get; init; }

    public decimal? MinimumReviewScore { get; init; }

    public List<Guid>? Amenities { get; init; }

    public string? CancellationPolicy { get; init; }

    public PropertySortBy SortBy { get; init; } = PropertySortBy.Recommended;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;

    public SearchPropertiesRequest ToUseCaseRequest() => new()
    {
        Destination = Destination,
        CheckInDate = CheckInDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
        CheckOutDate = CheckOutDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
        Adults = Adults,
        Children = Children,
        Rooms = Rooms,
        PropertyType = PropertyType,
        MinPrice = MinPrice,
        MaxPrice = MaxPrice,
        MinimumRating = MinimumRating,
        MinimumReviewScore = MinimumReviewScore,
        Amenities = Amenities,
        CancellationPolicy = CancellationPolicy,
        SortBy = SortBy,
        Page = Page,
        PageSize = PageSize
    };
}
