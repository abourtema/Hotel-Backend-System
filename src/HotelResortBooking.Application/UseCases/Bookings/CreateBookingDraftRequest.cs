namespace HotelResortBooking.Application.UseCases.Bookings;

public sealed class CreateBookingDraftRequest
{
    public Guid PropertyId { get; init; }

    public Guid RoomTypeId { get; init; }

    public DateOnly CheckInDate { get; init; }

    public DateOnly CheckOutDate { get; init; }

    public int Adults { get; init; } = 1;

    public int Children { get; init; }

    public int Rooms { get; init; } = 1;

    public IReadOnlyList<BookingUpgradeSelection> Upgrades { get; init; } = Array.Empty<BookingUpgradeSelection>();

    public int PointsToRedeem { get; init; }
}
