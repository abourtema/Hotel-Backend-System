namespace HotelResortBooking.Application.UseCases.Rooms;

public sealed class GetAvailableRoomsRequest
{
    public Guid PropertyId { get; init; }

    public DateOnly CheckInDate { get; init; }

    public DateOnly CheckOutDate { get; init; }

    public int Adults { get; init; } = 1;

    public int Children { get; init; }

    public int Rooms { get; init; } = 1;
}
