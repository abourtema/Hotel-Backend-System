namespace HotelResortBooking.Application.UseCases.Bookings;

/// <summary>
/// In-memory booking draft with server-side pricing.
/// Guests may create drafts while browsing; finalizing via CreateBooking requires authentication.
/// </summary>
public sealed class CreateBookingDraftResponse
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

    public string Currency { get; init; } = string.Empty;

    public decimal PricePerNight { get; init; }

    public decimal SubTotal { get; init; }

    public decimal UpgradeTotal { get; init; }

    public decimal DiscountAmount { get; init; }

    public int PointsRedeemed { get; init; }

    public decimal PointsDiscount { get; init; }

    public decimal TaxAmount { get; init; }

    public decimal TotalAmount { get; init; }

    public bool RequiresAuthenticationToFinalize { get; init; } = true;

    public IReadOnlyList<PriceBreakdownLineDto> PriceBreakdown { get; init; } = Array.Empty<PriceBreakdownLineDto>();

    public IReadOnlyList<BookingDraftUpgradeDto> Upgrades { get; init; } = Array.Empty<BookingDraftUpgradeDto>();
}

public sealed class BookingDraftUpgradeDto
{
    public Guid UpgradeOptionId { get; init; }

    public string Name { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal TotalPrice { get; init; }

    public bool IsMandatory { get; init; }
}
