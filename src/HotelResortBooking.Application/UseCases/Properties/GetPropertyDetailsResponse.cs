using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.UseCases.Properties;

public sealed class GetPropertyDetailsResponse
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
