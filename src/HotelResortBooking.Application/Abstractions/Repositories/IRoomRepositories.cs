using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.Abstractions.Repositories;

public sealed class AvailableRoomData
{
    public Guid RoomTypeId { get; init; }

    public Guid PropertyId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int MaxAdults { get; init; }

    public int MaxChildren { get; init; }

    public int MaxGuests { get; init; }

    public string? BedType { get; init; }

    public decimal PricePerNight { get; init; }

    public string Currency { get; init; } = string.Empty;

    public int AvailableCount { get; init; }

    public IReadOnlyList<PropertyImageData> Images { get; init; } = Array.Empty<PropertyImageData>();

    public IReadOnlyList<UpgradeOptionData> UpgradeOptions { get; init; } = Array.Empty<UpgradeOptionData>();
}

public interface IRoomTypeRepository
{
    Task<AvailableRoomData?> GetByIdAsync(Guid roomTypeId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AvailableRoomData>> GetAvailableRoomsAsync(
        Guid propertyId,
        DateOnly checkIn,
        DateOnly checkOut,
        int roomsRequested,
        CancellationToken cancellationToken = default);
}

public sealed class InventoryAvailabilityResult
{
    public bool IsAvailable { get; init; }

    public int MinimumAvailableRooms { get; init; }
}

public interface IRoomInventoryRepository
{
    Task<InventoryAvailabilityResult> CheckAvailabilityAsync(
        Guid roomTypeId,
        DateOnly checkIn,
        DateOnly checkOut,
        int roomsRequested,
        CancellationToken cancellationToken = default);

    Task<bool> ReserveAsync(
        Guid roomTypeId,
        DateOnly checkIn,
        DateOnly checkOut,
        int roomsRequested,
        CancellationToken cancellationToken = default);

    Task ReleaseAsync(
        Guid roomTypeId,
        DateOnly checkIn,
        DateOnly checkOut,
        int roomsRequested,
        CancellationToken cancellationToken = default);
}

public interface IUpgradeRepository
{
    Task<UpgradeOptionData?> GetByIdAsync(Guid upgradeOptionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UpgradeOptionData>> GetForPropertyAsync(
        Guid propertyId,
        Guid? roomTypeId = null,
        CancellationToken cancellationToken = default);
}
