using HotelResortBooking.Application.UseCases.Rooms;

namespace HotelResortBooking.Api.Contracts.Rooms;

public sealed class GetAvailableRoomsQueryParams
{
    public DateOnly? CheckInDate { get; init; }

    public DateOnly? CheckOutDate { get; init; }

    public int Adults { get; init; } = 1;

    public int Children { get; init; }

    public int Rooms { get; init; } = 1;

    public GetAvailableRoomsRequest ToUseCaseRequest(Guid propertyId) => new()
    {
        PropertyId = propertyId,
        CheckInDate = CheckInDate ?? DateOnly.FromDateTime(DateTime.UtcNow),
        CheckOutDate = CheckOutDate ?? DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
        Adults = Adults,
        Children = Children,
        Rooms = Rooms
    };
}
