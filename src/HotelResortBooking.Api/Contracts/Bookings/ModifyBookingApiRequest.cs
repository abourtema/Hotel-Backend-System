using HotelResortBooking.Application.UseCases.Bookings;

namespace HotelResortBooking.Api.Contracts.Bookings;

public sealed class ModifyBookingApiRequest
{
    public DateOnly CheckInDate { get; init; }

    public DateOnly CheckOutDate { get; init; }

    public int Adults { get; init; } = 1;

    public int Children { get; init; }

    public int Rooms { get; init; } = 1;

    public Guid? RoomTypeId { get; init; }

    public IReadOnlyList<BookingUpgradeSelection>? Upgrades { get; init; }

    public ModifyBookingRequest ToUseCaseRequest(Guid bookingId) => new()
    {
        BookingId = bookingId,
        CheckInDate = CheckInDate,
        CheckOutDate = CheckOutDate,
        Adults = Adults,
        Children = Children,
        Rooms = Rooms,
        RoomTypeId = RoomTypeId,
        Upgrades = Upgrades
    };
}
