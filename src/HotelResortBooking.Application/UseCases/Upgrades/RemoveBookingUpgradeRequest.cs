namespace HotelResortBooking.Application.UseCases.Upgrades;

public sealed class RemoveBookingUpgradeRequest
{
    public Guid BookingId { get; init; }

    public Guid UpgradeOptionId { get; init; }
}
