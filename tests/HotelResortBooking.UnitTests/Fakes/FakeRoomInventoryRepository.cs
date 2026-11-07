using HotelResortBooking.Application.Abstractions.Repositories;

namespace HotelResortBooking.UnitTests.Fakes;

public sealed class FakeRoomInventoryRepository : IRoomInventoryRepository
{
    private readonly Dictionary<(Guid RoomTypeId, DateOnly Date), int> _availability = new();

    public void Seed(Guid roomTypeId, DateOnly fromInclusive, DateOnly toExclusive, int available)
    {
        for (var date = fromInclusive; date < toExclusive; date = date.AddDays(1))
        {
            _availability[(roomTypeId, date)] = available;
        }
    }

    public void Set(Guid roomTypeId, DateOnly date, int available)
        => _availability[(roomTypeId, date)] = available;

    public InventoryAvailabilityResult CheckAvailabilitySync(
        Guid roomTypeId,
        DateOnly checkIn,
        DateOnly checkOut,
        int roomsRequested)
    {
        if (checkOut <= checkIn || roomsRequested <= 0)
        {
            return new InventoryAvailabilityResult { IsAvailable = false, MinimumAvailableRooms = 0 };
        }

        var min = int.MaxValue;
        for (var date = checkIn; date < checkOut; date = date.AddDays(1))
        {
            if (!_availability.TryGetValue((roomTypeId, date), out var available))
            {
                return new InventoryAvailabilityResult { IsAvailable = false, MinimumAvailableRooms = 0 };
            }

            min = Math.Min(min, available);
        }

        if (min == int.MaxValue)
        {
            min = 0;
        }

        return new InventoryAvailabilityResult
        {
            IsAvailable = min >= roomsRequested,
            MinimumAvailableRooms = min
        };
    }

    public Task<InventoryAvailabilityResult> CheckAvailabilityAsync(
        Guid roomTypeId,
        DateOnly checkIn,
        DateOnly checkOut,
        int roomsRequested,
        CancellationToken cancellationToken = default)
        => Task.FromResult(CheckAvailabilitySync(roomTypeId, checkIn, checkOut, roomsRequested));

    public Task<bool> ReserveAsync(
        Guid roomTypeId,
        DateOnly checkIn,
        DateOnly checkOut,
        int roomsRequested,
        CancellationToken cancellationToken = default)
    {
        var check = CheckAvailabilitySync(roomTypeId, checkIn, checkOut, roomsRequested);
        if (!check.IsAvailable)
        {
            return Task.FromResult(false);
        }

        for (var date = checkIn; date < checkOut; date = date.AddDays(1))
        {
            _availability[(roomTypeId, date)] -= roomsRequested;
        }

        return Task.FromResult(true);
    }

    public Task ReleaseAsync(
        Guid roomTypeId,
        DateOnly checkIn,
        DateOnly checkOut,
        int roomsRequested,
        CancellationToken cancellationToken = default)
    {
        for (var date = checkIn; date < checkOut; date = date.AddDays(1))
        {
            var key = (roomTypeId, date);
            if (_availability.TryGetValue(key, out var current))
            {
                _availability[key] = current + roomsRequested;
            }
            else
            {
                _availability[key] = roomsRequested;
            }
        }

        return Task.CompletedTask;
    }
}
