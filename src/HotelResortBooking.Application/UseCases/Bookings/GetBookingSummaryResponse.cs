namespace HotelResortBooking.Application.UseCases.Bookings;

public sealed class GetBookingSummaryResponse
{
    public Guid PropertyId { get; init; }

    public string PropertyName { get; init; } = string.Empty;

    public Guid RoomTypeId { get; init; }

    public string RoomTypeName { get; init; } = string.Empty;

    public DateOnly CheckInDate { get; init; }

    public DateOnly CheckOutDate { get; init; }

    public int Adults { get; init; }

    public int Children { get; init; }

    public int RoomsCount { get; init; }

    public int Nights { get; init; }

    public bool IsAvailable { get; init; }

    public int AvailableRooms { get; init; }

    public string Currency { get; init; } = string.Empty;

    public decimal PricePerNight { get; init; }

    public decimal SubTotal { get; init; }

    public decimal UpgradeTotal { get; init; }

    public decimal DiscountAmount { get; init; }

    public int PointsRedeemed { get; init; }

    public decimal PointsDiscount { get; init; }

    public decimal TaxAmount { get; init; }

    public decimal TotalAmount { get; init; }

    public IReadOnlyList<PriceBreakdownLineDto> PriceBreakdown { get; init; } = Array.Empty<PriceBreakdownLineDto>();

    public IReadOnlyList<BookingDraftUpgradeDto> Upgrades { get; init; } = Array.Empty<BookingDraftUpgradeDto>();
}
