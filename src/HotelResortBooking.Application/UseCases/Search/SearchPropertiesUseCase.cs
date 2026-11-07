using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Domain.Rules;

namespace HotelResortBooking.Application.UseCases.Search;

public sealed class SearchPropertiesUseCase :
    ISearchPropertiesUseCase,
    IFilterPropertiesUseCase,
    IGetPropertySearchResultsUseCase
{
    private readonly IPropertyRepository _propertyRepository;

    public SearchPropertiesUseCase(IPropertyRepository propertyRepository)
    {
        _propertyRepository = propertyRepository;
    }

    public async Task<Result<SearchPropertiesResponse>> ExecuteAsync(
        SearchPropertiesRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = Validate(request);
        if (validationError is not null)
        {
            return Result.Failure<SearchPropertiesResponse>(validationError);
        }

        if (!string.IsNullOrWhiteSpace(request.Destination))
        {
            var destinationExists = await _propertyRepository.DestinationExistsAsync(
                request.Destination.Trim(),
                cancellationToken);

            if (!destinationExists)
            {
                return Result.Failure<SearchPropertiesResponse>(
                    BusinessErrors.UnsupportedDestination(request.Destination.Trim()));
            }
        }

        var page = await _propertyRepository.SearchAsync(ToCriteria(request), cancellationToken);

        return Result.Success(new SearchPropertiesResponse
        {
            Items = page.Items,
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
            TotalPages = page.TotalPages
        });
    }

    private static Error? Validate(SearchPropertiesRequest request)
    {
        if (new DateRangeMustBeValidRule(request.CheckInDate, request.CheckOutDate).IsBroken())
        {
            return BusinessErrors.InvalidDateRange();
        }

        if (new GuestCountMustBeValidRule(request.Adults, request.Children).IsBroken())
        {
            return BusinessErrors.InvalidGuestCount();
        }

        if (new RoomCountMustBeValidRule(request.Rooms).IsBroken())
        {
            return BusinessErrors.InvalidRoomCount();
        }

        if (request.MinPrice is not null && request.MaxPrice is not null && request.MinPrice > request.MaxPrice)
        {
            return BusinessErrors.InvalidPriceRange();
        }

        if (request.Page <= 0 || request.PageSize <= 0)
        {
            return BusinessErrors.InvalidPagination();
        }

        return null;
    }

    private static PropertySearchCriteria ToCriteria(SearchPropertiesRequest request) => new()
    {
        Destination = request.Destination?.Trim(),
        CheckInDate = request.CheckInDate,
        CheckOutDate = request.CheckOutDate,
        Adults = request.Adults,
        Children = request.Children,
        Rooms = request.Rooms,
        PropertyType = request.PropertyType,
        MinPrice = request.MinPrice,
        MaxPrice = request.MaxPrice,
        MinimumStarRating = request.MinimumRating,
        MinimumReviewScore = request.MinimumReviewScore,
        AmenityIds = request.Amenities,
        CancellationPolicyContains = request.CancellationPolicy,
        SortBy = request.SortBy,
        Page = request.Page,
        PageSize = request.PageSize
    };
}
