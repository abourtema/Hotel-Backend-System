using HotelResortBooking.Application.UseCases.Map;

namespace HotelResortBooking.Api.Contracts.Map;

public sealed class SearchMapQueryParams
{
    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public double RadiusKm { get; init; } = 25;

    public DateOnly? CheckInDate { get; init; }

    public DateOnly? CheckOutDate { get; init; }

    public int Adults { get; init; } = 1;

    public int Children { get; init; }

    public int Rooms { get; init; } = 1;

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 50;

    public SearchPropertiesByLocationRequest ToUseCaseRequest()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new SearchPropertiesByLocationRequest
        {
            Latitude = Latitude,
            Longitude = Longitude,
            RadiusKm = RadiusKm,
            CheckInDate = CheckInDate ?? today,
            CheckOutDate = CheckOutDate ?? today.AddDays(1),
            Adults = Adults,
            Children = Children,
            Rooms = Rooms,
            Page = Page,
            PageSize = PageSize
        };
    }
}
