using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Domain.Rules;

namespace HotelResortBooking.Application.UseCases.Map;

public sealed class SearchPropertiesByLocationUseCase : ISearchPropertiesByLocationUseCase
{
    private readonly IPropertyRepository _propertyRepository;

    public SearchPropertiesByLocationUseCase(IPropertyRepository propertyRepository)
    {
        _propertyRepository = propertyRepository;
    }

    public async Task<Result<SearchPropertiesByLocationResponse>> ExecuteAsync(
        SearchPropertiesByLocationRequest request,
        CancellationToken cancellationToken = default)
    {
        if (double.IsNaN(request.Latitude) || double.IsNaN(request.Longitude)
            || request.Latitude is < -90 or > 90
            || request.Longitude is < -180 or > 180
            || request.RadiusKm <= 0)
        {
            return Result.Failure<SearchPropertiesByLocationResponse>(BusinessErrors.InvalidLocation());
        }

        if (new DateRangeMustBeValidRule(request.CheckInDate, request.CheckOutDate).IsBroken())
        {
            return Result.Failure<SearchPropertiesByLocationResponse>(BusinessErrors.InvalidDateRange());
        }

        if (new GuestCountMustBeValidRule(request.Adults, request.Children).IsBroken())
        {
            return Result.Failure<SearchPropertiesByLocationResponse>(BusinessErrors.InvalidGuestCount());
        }

        if (new RoomCountMustBeValidRule(request.Rooms).IsBroken())
        {
            return Result.Failure<SearchPropertiesByLocationResponse>(BusinessErrors.InvalidRoomCount());
        }

        if (request.MinPrice is not null && request.MaxPrice is not null && request.MinPrice > request.MaxPrice)
        {
            return Result.Failure<SearchPropertiesByLocationResponse>(BusinessErrors.InvalidPriceRange());
        }

        if (request.Page <= 0 || request.PageSize <= 0)
        {
            return Result.Failure<SearchPropertiesByLocationResponse>(BusinessErrors.InvalidPagination());
        }

        var page = await _propertyRepository.SearchByLocationAsync(new PropertySearchCriteria
        {
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            RadiusKm = request.RadiusKm,
            CheckInDate = request.CheckInDate,
            CheckOutDate = request.CheckOutDate,
            Adults = request.Adults,
            Children = request.Children,
            Rooms = request.Rooms,
            PropertyType = request.PropertyType,
            MinPrice = request.MinPrice,
            MaxPrice = request.MaxPrice,
            Page = request.Page,
            PageSize = request.PageSize
        }, cancellationToken);

        return Result.Success(new SearchPropertiesByLocationResponse
        {
            Pins = page.Items.Select(item => new MapPropertyPinDto
            {
                PropertyId = item.PropertyId,
                PropertyName = item.PropertyName,
                MainImageUrl = item.MainImageUrl,
                Latitude = item.Latitude,
                Longitude = item.Longitude,
                PricePerNight = item.PricePerNight,
                TotalPrice = item.TotalPrice,
                Currency = item.Currency,
                ReviewScore = item.ReviewScore,
                StarRating = item.StarRating,
                City = item.City
            }).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount,
            TotalPages = page.TotalPages
        });
    }
}
