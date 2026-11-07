using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.UseCases.Map;

public sealed class SearchPropertiesByLocationRequest
{
    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public double RadiusKm { get; init; } = 10;

    public DateOnly CheckInDate { get; init; }

    public DateOnly CheckOutDate { get; init; }

    public int Adults { get; init; } = 1;

    public int Children { get; init; }

    public int Rooms { get; init; } = 1;

    public PropertyType? PropertyType { get; init; }

    public decimal? MinPrice { get; init; }

    public decimal? MaxPrice { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 50;
}
