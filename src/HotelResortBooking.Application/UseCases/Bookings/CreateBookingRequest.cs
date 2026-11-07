using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.UseCases.Bookings;

public sealed class CreateBookingRequest
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

    public BookingGuestInput Guest { get; init; } = new();

    public IReadOnlyList<BookingGuestInput> AdditionalGuests { get; init; } = Array.Empty<BookingGuestInput>();

    public string? SpecialRequests { get; init; }

    public BookingCheckoutPaymentInput? Payment { get; init; }
}

public sealed class BookingGuestInput
{
    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string? Email { get; init; }

    public string? PhoneNumber { get; init; }
}

public sealed class BookingCheckoutPaymentInput
{
    public PaymentMethodType? Method { get; init; }

    /// <summary>
    /// Token from the payment provider. Never send the raw card number or CVV.
    /// </summary>
    public string? PaymentMethodToken { get; init; }

    public string? CardBrand { get; init; }

    public string? CardLast4 { get; init; }

    public string? CardholderName { get; init; }

    public string? IdempotencyKey { get; init; }
}
