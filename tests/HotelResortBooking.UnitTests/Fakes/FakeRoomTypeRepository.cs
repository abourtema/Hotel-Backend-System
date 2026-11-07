using HotelResortBooking.Application.Abstractions.Repositories;

namespace HotelResortBooking.UnitTests.Fakes;

public sealed class FakeRoomTypeRepository : IRoomTypeRepository
{
    public List<AvailableRoomData> Rooms { get; } = [];

    /// <summary>
    /// When set, GetAvailableRoomsAsync only returns rooms whose AvailableCount meets roomsRequested
    /// and whose RoomTypeId is present for the date range in this inventory fake.
    /// </summary>
    public FakeRoomInventoryRepository? Inventory { get; set; }

    public Task<AvailableRoomData?> GetByIdAsync(Guid roomTypeId, CancellationToken cancellationToken = default)
        => Task.FromResult(Rooms.FirstOrDefault(r => r.RoomTypeId == roomTypeId));

    public Task<IReadOnlyList<AvailableRoomData>> GetAvailableRoomsAsync(
        Guid propertyId,
        DateOnly checkIn,
        DateOnly checkOut,
        int roomsRequested,
        CancellationToken cancellationToken = default)
    {
        var results = new List<AvailableRoomData>();

        foreach (var room in Rooms.Where(r => r.PropertyId == propertyId))
        {
            if (Inventory is not null)
            {
                var availability = Inventory.CheckAvailabilitySync(room.RoomTypeId, checkIn, checkOut, roomsRequested);
                if (!availability.IsAvailable)
                {
                    continue;
                }

                results.Add(CloneWithAvailable(room, availability.MinimumAvailableRooms));
            }
            else if (room.AvailableCount >= roomsRequested)
            {
                results.Add(room);
            }
        }

        return Task.FromResult<IReadOnlyList<AvailableRoomData>>(results);
    }

    private static AvailableRoomData CloneWithAvailable(AvailableRoomData room, int available) => new()
    {
        RoomTypeId = room.RoomTypeId,
        PropertyId = room.PropertyId,
        Name = room.Name,
        Description = room.Description,
        MaxAdults = room.MaxAdults,
        MaxChildren = room.MaxChildren,
        MaxGuests = room.MaxGuests,
        BedType = room.BedType,
        PricePerNight = room.PricePerNight,
        Currency = room.Currency,
        AvailableCount = available,
        Images = room.Images,
        UpgradeOptions = room.UpgradeOptions
    };
}
