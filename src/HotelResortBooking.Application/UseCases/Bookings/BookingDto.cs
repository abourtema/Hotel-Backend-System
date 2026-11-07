using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.UseCases.Bookings;

public sealed class BookingDto
{
    public Guid Id { get; init; }

    public string BookingNumber { get; init; } = string.Empty;

    public Guid? UserId { get; init; }

    public Guid PropertyId { get; init; }

    public string PropertyName { get; init; } = string.Empty;

    public DateOnly CheckInDate { get; init; }

    public DateOnly CheckOutDate { get; init; }

    public int Adults { get; init; }

    public int Children { get; init; }

    public int RoomsCount { get; init; }

    public BookingStatus Status { get; init; }

    public string Currency { get; init; } = string.Empty;

    public decimal SubTotal { get; init; }

    public decimal UpgradeTotal { get; init; }

    public decimal DiscountAmount { get; init; }

    public int PointsRedeemed { get; init; }

    public decimal TaxAmount { get; init; }

    public decimal TotalAmount { get; init; }

    public DateTimeOffset? CancellationDeadline { get; init; }

    public DateTimeOffset? ConfirmedAt { get; init; }

    public DateTimeOffset? CancelledAt { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public string? SpecialRequests { get; init; }

    public BookingGuestDto? Guest { get; init; }

    public IReadOnlyList<BookingGuestDto> AdditionalGuests { get; init; } = Array.Empty<BookingGuestDto>();

    public IReadOnlyList<BookingRoomDto> Rooms { get; init; } = Array.Empty<BookingRoomDto>();

    public IReadOnlyList<BookingUpgradeDto> Upgrades { get; init; } = Array.Empty<BookingUpgradeDto>();

    public Guid? PaymentId { get; init; }

    public PaymentStatus? PaymentStatus { get; init; }

    public PaymentMethodType? PaymentMethod { get; init; }

    public static BookingDto From(BookingData data, PaymentData? payment = null) => new()
    {
        Id = data.Id,
        BookingNumber = data.BookingNumber,
        UserId = data.UserId,
        PropertyId = data.PropertyId,
        PropertyName = data.PropertyName,
        CheckInDate = data.CheckInDate,
        CheckOutDate = data.CheckOutDate,
        Adults = data.Adults,
        Children = data.Children,
        RoomsCount = data.RoomsCount,
        Status = data.Status,
        Currency = data.Currency,
        SubTotal = data.SubTotal,
        UpgradeTotal = data.UpgradeTotal,
        DiscountAmount = data.DiscountAmount,
        PointsRedeemed = data.PointsRedeemed,
        TaxAmount = data.TaxAmount,
        TotalAmount = data.TotalAmount,
        CancellationDeadline = data.CancellationDeadline,
        ConfirmedAt = data.ConfirmedAt,
        CancelledAt = data.CancelledAt,
        CreatedAt = data.CreatedAt,
        SpecialRequests = data.SpecialRequests,
        Guest = data.Guests
            .Where(g => g.IsPrimary)
            .OrderBy(g => g.SortOrder)
            .Select(MapGuest)
            .FirstOrDefault(),
        AdditionalGuests = data.Guests
            .Where(g => !g.IsPrimary)
            .OrderBy(g => g.SortOrder)
            .Select(MapGuest)
            .ToList(),
        Rooms = data.Rooms.Select(r => new BookingRoomDto
        {
            Id = r.Id,
            RoomTypeId = r.RoomTypeId,
            RoomTypeName = r.RoomTypeName,
            Quantity = r.Quantity,
            PricePerNight = r.PricePerNight,
            Nights = r.Nights,
            SubTotal = r.SubTotal
        }).ToList(),
        Upgrades = data.Upgrades.Select(u => new BookingUpgradeDto
        {
            Id = u.Id,
            UpgradeOptionId = u.UpgradeOptionId,
            Name = u.Name,
            Quantity = u.Quantity,
            UnitPrice = u.UnitPrice,
            TotalPrice = u.TotalPrice,
            IsMandatory = u.IsMandatory
        }).ToList(),
        PaymentId = payment?.Id,
        PaymentStatus = payment?.Status,
        PaymentMethod = payment?.PaymentMethod
    };

    public static PaymentData? SelectDisplayPayment(IEnumerable<PaymentData> payments)
    {
        var list = payments as IReadOnlyList<PaymentData> ?? payments.ToList();
        if (list.Count == 0)
        {
            return null;
        }

        return list
                .Where(p => p.Status == Domain.Enums.PaymentStatus.Succeeded)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefault()
            ?? list.OrderByDescending(p => p.CreatedAt).First();
    }

    public static IReadOnlyDictionary<Guid, PaymentData> IndexDisplayPayments(IEnumerable<PaymentData> payments)
        => payments
            .GroupBy(p => p.BookingId)
            .Select(group => (group.Key, Payment: SelectDisplayPayment(group)))
            .Where(item => item.Payment is not null)
            .ToDictionary(item => item.Key, item => item.Payment!);

    private static BookingGuestDto MapGuest(BookingGuestData guest) => new()
    {
        Id = guest.Id,
        FirstName = guest.FirstName,
        LastName = guest.LastName,
        Email = guest.Email,
        PhoneNumber = guest.PhoneNumber,
        IsPrimary = guest.IsPrimary
    };
}

public sealed class BookingGuestDto
{
    public Guid Id { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string? Email { get; init; }

    public string? PhoneNumber { get; init; }

    public bool IsPrimary { get; init; }
}

public sealed class BookingRoomDto
{
    public Guid Id { get; init; }

    public Guid RoomTypeId { get; init; }

    public string RoomTypeName { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public decimal PricePerNight { get; init; }

    public int Nights { get; init; }

    public decimal SubTotal { get; init; }
}

public sealed class BookingUpgradeDto
{
    public Guid Id { get; init; }

    public Guid UpgradeOptionId { get; init; }

    public string Name { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal TotalPrice { get; init; }

    public bool IsMandatory { get; init; }
}

public sealed class BookingUpgradeSelection
{
    public Guid UpgradeOptionId { get; init; }

    public int Quantity { get; init; } = 1;
}
