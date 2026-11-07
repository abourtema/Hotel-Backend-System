namespace HotelResortBooking.Application.UseCases.Upgrades;

public sealed class AddBookingUpgradeRequest
{
    public Guid BookingId { get; init; }

    public Guid UpgradeOptionId { get; init; }

    public int Quantity { get; init; } = 1;
}
