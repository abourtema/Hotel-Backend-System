using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Abstractions.Services;

namespace HotelResortBooking.Application.Services;

public sealed class RoomAvailabilityService : IRoomAvailabilityService
{
    private readonly IRoomInventoryRepository _inventoryRepository;

    public RoomAvailabilityService(IRoomInventoryRepository inventoryRepository)
    {
        _inventoryRepository = inventoryRepository;
    }

    public async Task<AvailabilityCheckResult> CheckAsync(
        AvailabilityCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await _inventoryRepository.CheckAvailabilityAsync(
            request.RoomTypeId,
            request.CheckInDate,
            request.CheckOutDate,
            request.RoomsRequested,
            cancellationToken);

        return new AvailabilityCheckResult
        {
            IsAvailable = result.IsAvailable,
            AvailableRooms = result.MinimumAvailableRooms
        };
    }

    public Task<bool> ReserveAsync(
        AvailabilityCheckRequest request,
        CancellationToken cancellationToken = default)
        => _inventoryRepository.ReserveAsync(
            request.RoomTypeId,
            request.CheckInDate,
            request.CheckOutDate,
            request.RoomsRequested,
            cancellationToken);

    public Task ReleaseAsync(
        AvailabilityCheckRequest request,
        CancellationToken cancellationToken = default)
        => _inventoryRepository.ReleaseAsync(
            request.RoomTypeId,
            request.CheckInDate,
            request.CheckOutDate,
            request.RoomsRequested,
            cancellationToken);
}
