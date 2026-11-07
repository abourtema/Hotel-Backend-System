using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.UnitTests.Fakes;

public sealed class FakeBookingRepository : IBookingRepository
{
    public List<BookingData> Bookings { get; } = [];
    public List<(Guid BookingId, BookingStatus? OldStatus, BookingStatus NewStatus, string? Reason)> StatusHistory { get; } = [];

    public string? PropertyNameOverride { get; set; }

    public Task<BookingData?> GetByIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
        => Task.FromResult(Bookings.FirstOrDefault(b => b.Id == bookingId));

    public Task<BookingData?> GetByBookingNumberAsync(string bookingNumber, CancellationToken cancellationToken = default)
        => Task.FromResult(Bookings.FirstOrDefault(b =>
            string.Equals(b.BookingNumber, bookingNumber, StringComparison.OrdinalIgnoreCase)));

    public Task<IReadOnlyList<BookingData>> GetUserBookingsAsync(
        Guid userId,
        BookingListGroup? group,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var items = Bookings.Where(b => b.UserId == userId).ToList();
        return Task.FromResult<IReadOnlyList<BookingData>>(items);
    }

    public Task<BookingData> AddAsync(CreateBookingModel model, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid();
        var booking = new BookingData
        {
            Id = id,
            BookingNumber = $"BK{id.ToString("N")[..8].ToUpperInvariant()}",
            UserId = model.UserId,
            PropertyId = model.PropertyId,
            PropertyName = PropertyNameOverride ?? "Property",
            CheckInDate = model.CheckInDate,
            CheckOutDate = model.CheckOutDate,
            Adults = model.Adults,
            Children = model.Children,
            RoomsCount = model.RoomsCount,
            Status = model.Status,
            Currency = model.Currency,
            SubTotal = model.SubTotal,
            UpgradeTotal = model.UpgradeTotal,
            DiscountAmount = model.DiscountAmount,
            PointsRedeemed = model.PointsRedeemed,
            TaxAmount = model.TaxAmount,
            TotalAmount = model.TotalAmount,
            CancellationDeadline = model.CancellationDeadline,
            ConfirmedAt = model.ConfirmedAt,
            CreatedAt = DateTimeOffset.UtcNow,
            SpecialRequests = model.SpecialRequests,
            Guests = model.Guests.Select(g => new BookingGuestData
            {
                Id = Guid.NewGuid(),
                FirstName = g.FirstName,
                LastName = g.LastName,
                Email = g.Email,
                PhoneNumber = g.PhoneNumber,
                IsPrimary = g.IsPrimary,
                SortOrder = g.SortOrder
            }).ToList(),
            Rooms = model.Rooms.Select(r => new BookingRoomData
            {
                Id = Guid.NewGuid(),
                RoomTypeId = r.RoomTypeId,
                RoomTypeName = "Room",
                Quantity = r.Quantity,
                PricePerNight = r.PricePerNight,
                Nights = r.Nights,
                SubTotal = r.SubTotal
            }).ToList(),
            Upgrades = model.Upgrades.Select(u => new BookingUpgradeData
            {
                Id = Guid.NewGuid(),
                UpgradeOptionId = u.UpgradeOptionId,
                Name = "Upgrade",
                Quantity = u.Quantity,
                UnitPrice = u.UnitPrice,
                TotalPrice = u.TotalPrice,
                IsMandatory = false
            }).ToList()
        };

        Bookings.Add(booking);
        return Task.FromResult(booking);
    }

    public Task UpdateAsync(BookingData booking, CancellationToken cancellationToken = default)
    {
        var index = Bookings.FindIndex(b => b.Id == booking.Id);
        if (index >= 0)
        {
            Bookings[index] = booking;
        }
        else
        {
            Bookings.Add(booking);
        }

        return Task.CompletedTask;
    }

    public Task AddStatusHistoryAsync(
        Guid bookingId,
        BookingStatus? oldStatus,
        BookingStatus newStatus,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        StatusHistory.Add((bookingId, oldStatus, newStatus, reason));
        return Task.CompletedTask;
    }

    public Task ReplaceUpgradesAsync(
        Guid bookingId,
        IReadOnlyList<CreateBookingUpgradeModel> upgrades,
        CancellationToken cancellationToken = default)
    {
        var booking = Bookings.FirstOrDefault(b => b.Id == bookingId);
        if (booking is null)
        {
            return Task.CompletedTask;
        }

        var updated = new BookingData
        {
            Id = booking.Id,
            BookingNumber = booking.BookingNumber,
            UserId = booking.UserId,
            PropertyId = booking.PropertyId,
            PropertyName = booking.PropertyName,
            CheckInDate = booking.CheckInDate,
            CheckOutDate = booking.CheckOutDate,
            Adults = booking.Adults,
            Children = booking.Children,
            RoomsCount = booking.RoomsCount,
            Status = booking.Status,
            Currency = booking.Currency,
            SubTotal = booking.SubTotal,
            UpgradeTotal = booking.UpgradeTotal,
            DiscountAmount = booking.DiscountAmount,
            PointsRedeemed = booking.PointsRedeemed,
            TaxAmount = booking.TaxAmount,
            TotalAmount = booking.TotalAmount,
            CancellationDeadline = booking.CancellationDeadline,
            ConfirmedAt = booking.ConfirmedAt,
            CancelledAt = booking.CancelledAt,
            CreatedAt = booking.CreatedAt,
            SpecialRequests = booking.SpecialRequests,
            Guests = booking.Guests,
            Rooms = booking.Rooms,
            Upgrades = upgrades.Select(u => new BookingUpgradeData
            {
                Id = Guid.NewGuid(),
                UpgradeOptionId = u.UpgradeOptionId,
                Name = "Upgrade",
                Quantity = u.Quantity,
                UnitPrice = u.UnitPrice,
                TotalPrice = u.TotalPrice,
                IsMandatory = false
            }).ToList()
        };

        return UpdateAsync(updated, cancellationToken);
    }

    public void Seed(BookingData booking) => Bookings.Add(booking);
}
