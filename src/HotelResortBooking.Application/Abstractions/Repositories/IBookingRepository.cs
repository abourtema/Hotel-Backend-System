using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.Abstractions.Repositories;

public sealed class BookingData
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

    public IReadOnlyList<BookingGuestData> Guests { get; init; } = Array.Empty<BookingGuestData>();

    public IReadOnlyList<BookingRoomData> Rooms { get; init; } = Array.Empty<BookingRoomData>();

    public IReadOnlyList<BookingUpgradeData> Upgrades { get; init; } = Array.Empty<BookingUpgradeData>();
}

public sealed class BookingGuestData
{
    public Guid Id { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string? Email { get; init; }

    public string? PhoneNumber { get; init; }

    public bool IsPrimary { get; init; }

    public int SortOrder { get; init; }
}

public sealed class BookingRoomData
{
    public Guid Id { get; init; }

    public Guid RoomTypeId { get; init; }

    public string RoomTypeName { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public decimal PricePerNight { get; init; }

    public int Nights { get; init; }

    public decimal SubTotal { get; init; }
}

public sealed class BookingUpgradeData
{
    public Guid Id { get; init; }

    public Guid UpgradeOptionId { get; init; }

    public string Name { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal TotalPrice { get; init; }

    public bool IsMandatory { get; init; }
}

public sealed class CreateBookingModel
{
    public Guid UserId { get; init; }

    public Guid PropertyId { get; init; }

    public DateOnly CheckInDate { get; init; }

    public DateOnly CheckOutDate { get; init; }

    public int Adults { get; init; }

    public int Children { get; init; }

    public int RoomsCount { get; init; }

    public BookingStatus Status { get; init; } = BookingStatus.Pending;

    public string Currency { get; init; } = string.Empty;

    public decimal SubTotal { get; init; }

    public decimal UpgradeTotal { get; init; }

    public decimal DiscountAmount { get; init; }

    public int PointsRedeemed { get; init; }

    public decimal TaxAmount { get; init; }

    public decimal TotalAmount { get; init; }

    public DateTimeOffset? CancellationDeadline { get; init; }

    public DateTimeOffset? ConfirmedAt { get; init; }

    public string? SpecialRequests { get; init; }

    public IReadOnlyList<CreateBookingGuestModel> Guests { get; init; } = Array.Empty<CreateBookingGuestModel>();

    public IReadOnlyList<CreateBookingRoomModel> Rooms { get; init; } = Array.Empty<CreateBookingRoomModel>();

    public IReadOnlyList<CreateBookingUpgradeModel> Upgrades { get; init; } = Array.Empty<CreateBookingUpgradeModel>();
}

public sealed class CreateBookingGuestModel
{
    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string? Email { get; init; }

    public string? PhoneNumber { get; init; }

    public bool IsPrimary { get; init; }

    public int SortOrder { get; init; }
}

public sealed class CreateBookingRoomModel
{
    public Guid RoomTypeId { get; init; }

    public int Quantity { get; init; }

    public decimal PricePerNight { get; init; }

    public int Nights { get; init; }

    public decimal SubTotal { get; init; }
}

public sealed class CreateBookingUpgradeModel
{
    public Guid UpgradeOptionId { get; init; }

    public int Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal TotalPrice { get; init; }
}

public enum BookingListGroup
{
    Upcoming = 0,
    Ongoing = 1,
    Past = 2
}

public interface IBookingRepository
{
    Task<BookingData?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task<BookingData?> GetByBookingNumberAsync(string bookingNumber, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookingData>> GetUserBookingsAsync(
        Guid userId,
        BookingListGroup? group,
        DateOnly today,
        CancellationToken cancellationToken = default);

    Task<BookingData> AddAsync(CreateBookingModel model, CancellationToken cancellationToken = default);

    Task UpdateAsync(BookingData booking, CancellationToken cancellationToken = default);

    Task AddStatusHistoryAsync(
        Guid bookingId,
        BookingStatus? oldStatus,
        BookingStatus newStatus,
        string? reason,
        CancellationToken cancellationToken = default);

    Task ReplaceUpgradesAsync(
        Guid bookingId,
        IReadOnlyList<CreateBookingUpgradeModel> upgrades,
        CancellationToken cancellationToken = default);
}
