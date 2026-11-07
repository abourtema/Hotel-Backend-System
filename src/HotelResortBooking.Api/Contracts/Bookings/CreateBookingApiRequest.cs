using System.Text.Json.Serialization;
using HotelResortBooking.Api.Serialization;
using HotelResortBooking.Application.UseCases.Bookings;
using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Api.Contracts.Bookings;

public sealed class CreateBookingApiRequest
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

    public BookingGuestApiRequest? Guest { get; init; }

    public IReadOnlyList<BookingGuestApiRequest> AdditionalGuests { get; init; } =
        Array.Empty<BookingGuestApiRequest>();

    public string? SpecialRequests { get; init; }

    [JsonConverter(typeof(FlexibleCheckoutPaymentJsonConverter))]
    public BookingCheckoutPaymentApiRequest? Payment { get; init; }

    /// <summary>Accepted when the app sends guest fields at the root instead of nested <c>guest</c>.</summary>
    public string? FirstName { get; init; }

    public string? LastName { get; init; }

    public string? Email { get; init; }

    public string? PhoneNumber { get; init; }

    public string? Phone { get; init; }

    /// <summary>Accepted when the app sends payment method at the root instead of nested <c>payment</c>.</summary>
    public PaymentMethodType? PaymentMethod { get; init; }

    /// <summary>Dart often serializes the enum as <c>paymentMethodType</c>.</summary>
    public PaymentMethodType? PaymentMethodType { get; init; }

    public string? PaymentMethodToken { get; init; }

    public string? CardBrand { get; init; }

    public string? CardLast4 { get; init; }

    public string? CardholderName { get; init; }

    public string? IdempotencyKey { get; init; }

    public CreateBookingRequest ToUseCaseRequest() => new()
    {
        PropertyId = PropertyId,
        RoomTypeId = RoomTypeId,
        CheckInDate = CheckInDate,
        CheckOutDate = CheckOutDate,
        Adults = Adults,
        Children = Children,
        Rooms = Rooms,
        Upgrades = Upgrades,
        PointsToRedeem = PointsToRedeem,
        Guest = ResolveGuest(),
        AdditionalGuests = AdditionalGuests.Select(g => g.ToInput()).ToList(),
        SpecialRequests = SpecialRequests,
        Payment = ResolvePayment()
    };

    public CreateBookingDraftRequest ToDraftRequest() => new()
    {
        PropertyId = PropertyId,
        RoomTypeId = RoomTypeId,
        CheckInDate = CheckInDate,
        CheckOutDate = CheckOutDate,
        Adults = Adults,
        Children = Children,
        Rooms = Rooms,
        Upgrades = Upgrades,
        PointsToRedeem = PointsToRedeem
    };

    public GetBookingSummaryRequest ToSummaryRequest() => new()
    {
        PropertyId = PropertyId,
        RoomTypeId = RoomTypeId,
        CheckInDate = CheckInDate,
        CheckOutDate = CheckOutDate,
        Adults = Adults,
        Children = Children,
        Rooms = Rooms,
        Upgrades = Upgrades,
        PointsToRedeem = PointsToRedeem
    };

    private BookingGuestInput ResolveGuest()
    {
        var nested = Guest?.ToInput() ?? new BookingGuestInput();
        return new BookingGuestInput
        {
            FirstName = FirstNonEmpty(nested.FirstName, FirstName),
            LastName = FirstNonEmpty(nested.LastName, LastName),
            Email = FirstNonEmpty(nested.Email, Email),
            PhoneNumber = FirstNonEmpty(nested.PhoneNumber, PhoneNumber, Phone)
        };
    }

    private BookingCheckoutPaymentInput? ResolvePayment()
    {
        var nested = Payment?.ToInput();
        var method = nested?.Method ?? PaymentMethod ?? PaymentMethodType;
        if (method is null
            && string.IsNullOrWhiteSpace(nested?.PaymentMethodToken ?? PaymentMethodToken)
            && string.IsNullOrWhiteSpace(nested?.IdempotencyKey ?? IdempotencyKey))
        {
            return null;
        }

        return new BookingCheckoutPaymentInput
        {
            Method = method ?? Domain.Enums.PaymentMethodType.CreditCard,
            PaymentMethodToken = FirstNonEmpty(nested?.PaymentMethodToken, PaymentMethodToken),
            CardBrand = FirstNonEmpty(nested?.CardBrand, CardBrand),
            CardLast4 = FirstNonEmpty(nested?.CardLast4, CardLast4),
            CardholderName = FirstNonEmpty(nested?.CardholderName, CardholderName),
            IdempotencyKey = FirstNonEmpty(nested?.IdempotencyKey, IdempotencyKey)
        };
    }

    private static string FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? string.Empty;
}

public sealed class BookingGuestApiRequest
{
    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string? Email { get; init; }

    public string? PhoneNumber { get; init; }

    public string? Phone { get; init; }

    public BookingGuestInput ToInput() => new()
    {
        FirstName = FirstName,
        LastName = LastName,
        Email = Email,
        PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? Phone : PhoneNumber
    };
}

public sealed class BookingCheckoutPaymentApiRequest
{
    public PaymentMethodType? Method { get; init; }

    public PaymentMethodType? Type { get; init; }

    public PaymentMethodType? PaymentMethod { get; init; }

    public PaymentMethodType? PaymentMethodType { get; init; }

    public string? PaymentMethodToken { get; init; }

    public string? CardBrand { get; init; }

    public string? CardLast4 { get; init; }

    public string? CardholderName { get; init; }

    public string? IdempotencyKey { get; init; }

    public BookingCheckoutPaymentInput ToInput() => new()
    {
        Method = Method ?? Type ?? PaymentMethod ?? PaymentMethodType,
        PaymentMethodToken = PaymentMethodToken,
        CardBrand = CardBrand,
        CardLast4 = CardLast4,
        CardholderName = CardholderName,
        IdempotencyKey = IdempotencyKey
    };
}
