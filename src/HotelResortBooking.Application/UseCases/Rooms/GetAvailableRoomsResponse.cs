using HotelResortBooking.Application.Abstractions.Repositories;

namespace HotelResortBooking.Application.UseCases.Rooms;

public sealed class GetAvailableRoomsResponse
{
    public Guid PropertyId { get; init; }

    public DateOnly CheckInDate { get; init; }

    public DateOnly CheckOutDate { get; init; }

    public int Nights { get; init; }

    public IReadOnlyList<AvailableRoomOptionDto> Rooms { get; init; } = Array.Empty<AvailableRoomOptionDto>();
}

public sealed class AvailableRoomOptionDto
{
    public Guid RoomTypeId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int MaxAdults { get; init; }

    public int MaxChildren { get; init; }

    public int MaxGuests { get; init; }

    public string? BedType { get; init; }

    public decimal PricePerNight { get; init; }

    public decimal TotalPrice { get; init; }

    public string Currency { get; init; } = string.Empty;

    public int AvailableCount { get; init; }

    public IReadOnlyList<PropertyImageData> Images { get; init; } = Array.Empty<PropertyImageData>();

    public IReadOnlyList<UpgradeOptionData> UpgradeOptions { get; init; } = Array.Empty<UpgradeOptionData>();
}
