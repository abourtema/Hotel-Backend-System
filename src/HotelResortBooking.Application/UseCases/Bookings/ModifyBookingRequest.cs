namespace HotelResortBooking.Application.UseCases.Bookings;

public sealed class ModifyBookingRequest
{
    public Guid BookingId { get; init; }

    public DateOnly CheckInDate { get; init; }

    public DateOnly CheckOutDate { get; init; }

    public int Adults { get; init; } = 1;

    public int Children { get; init; }

    public int Rooms { get; init; } = 1;

    public Guid? RoomTypeId { get; init; }

    public IReadOnlyList<BookingUpgradeSelection>? Upgrades { get; init; }

    public bool BypassOwnershipCheck { get; init; }
}
