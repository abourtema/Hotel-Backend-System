using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Domain.Entities;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelResortBooking.Infrastructure.Repositories;

public sealed class BookingRepository : IBookingRepository
{
    private readonly ApplicationDbContext _dbContext;

    public BookingRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<BookingData?> GetByIdAsync(
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        var booking = await Query()
            .FirstOrDefaultAsync(b => b.Id == bookingId, cancellationToken);

        return booking is null ? null : Map(booking);
    }

    public async Task<BookingData?> GetByBookingNumberAsync(
        string bookingNumber,
        CancellationToken cancellationToken = default)
    {
        var booking = await Query()
            .FirstOrDefaultAsync(b => b.BookingNumber == bookingNumber, cancellationToken);

        return booking is null ? null : Map(booking);
    }

    public async Task<IReadOnlyList<BookingData>> GetUserBookingsAsync(
        Guid userId,
        BookingListGroup? group,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var query = Query().Where(b => b.UserId == userId);

        query = group switch
        {
            BookingListGroup.Upcoming => query.Where(b => b.CheckInDate > today),
            BookingListGroup.Ongoing => query.Where(b => b.CheckInDate <= today && b.CheckOutDate > today),
            BookingListGroup.Past => query.Where(b => b.CheckOutDate <= today),
            _ => query
        };

        var bookings = await query
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync(cancellationToken);

        return bookings.Select(Map).ToList();
    }

    public async Task<BookingData> AddAsync(
        CreateBookingModel model,
        CancellationToken cancellationToken = default)
    {
        var bookingNumber =
            $"HRB-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";

        var booking = new Booking
        {
            BookingNumber = bookingNumber,
            UserId = model.UserId,
            PropertyId = model.PropertyId,
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
            SpecialRequests = model.SpecialRequests,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        foreach (var guest in model.Guests)
        {
            booking.Guests.Add(new BookingGuest
            {
                FirstName = guest.FirstName,
                LastName = guest.LastName,
                Email = guest.Email,
                PhoneNumber = guest.PhoneNumber,
                IsPrimary = guest.IsPrimary,
                SortOrder = guest.SortOrder,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        foreach (var room in model.Rooms)
        {
            booking.Rooms.Add(new BookingRoom
            {
                RoomTypeId = room.RoomTypeId,
                Quantity = room.Quantity,
                PricePerNight = room.PricePerNight,
                Nights = room.Nights,
                SubTotal = room.SubTotal,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        foreach (var upgrade in model.Upgrades)
        {
            booking.Upgrades.Add(new BookingUpgrade
            {
                UpgradeOptionId = upgrade.UpgradeOptionId,
                Quantity = upgrade.Quantity,
                UnitPrice = upgrade.UnitPrice,
                TotalPrice = upgrade.TotalPrice,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        booking.StatusHistory.Add(new BookingStatusHistory
        {
            OldStatus = null,
            NewStatus = model.Status,
            ChangedAt = DateTimeOffset.UtcNow,
            Reason = "Booking created"
        });

        _dbContext.Bookings.Add(booking);

        // Load navigation names for the returned DTO when available in change tracker / DB.
        await _dbContext.Entry(booking).Reference(b => b.Property).LoadAsync(cancellationToken);
        foreach (var room in booking.Rooms)
        {
            await _dbContext.Entry(room).Reference(r => r.RoomType).LoadAsync(cancellationToken);
        }

        foreach (var upgrade in booking.Upgrades)
        {
            await _dbContext.Entry(upgrade).Reference(u => u.UpgradeOption).LoadAsync(cancellationToken);
        }

        return Map(booking);
    }

    public async Task UpdateAsync(BookingData booking, CancellationToken cancellationToken = default)
    {
        var entity = _dbContext.Bookings.Local.FirstOrDefault(b => b.Id == booking.Id)
            ?? await _dbContext.Bookings.FirstOrDefaultAsync(b => b.Id == booking.Id, cancellationToken);

        if (entity is null)
        {
            throw new InvalidOperationException($"Booking '{booking.Id}' was not found.");
        }

        entity.UserId = booking.UserId;
        entity.PropertyId = booking.PropertyId;
        entity.CheckInDate = booking.CheckInDate;
        entity.CheckOutDate = booking.CheckOutDate;
        entity.Adults = booking.Adults;
        entity.Children = booking.Children;
        entity.RoomsCount = booking.RoomsCount;
        entity.Status = booking.Status;
        entity.Currency = booking.Currency;
        entity.SubTotal = booking.SubTotal;
        entity.UpgradeTotal = booking.UpgradeTotal;
        entity.DiscountAmount = booking.DiscountAmount;
        entity.PointsRedeemed = booking.PointsRedeemed;
        entity.TaxAmount = booking.TaxAmount;
        entity.TotalAmount = booking.TotalAmount;
        entity.CancellationDeadline = booking.CancellationDeadline;
        entity.ConfirmedAt = booking.ConfirmedAt;
        entity.CancelledAt = booking.CancelledAt;
        entity.SpecialRequests = booking.SpecialRequests;
    }

    public async Task AddStatusHistoryAsync(
        Guid bookingId,
        BookingStatus? oldStatus,
        BookingStatus newStatus,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        _dbContext.BookingStatusHistories.Add(new BookingStatusHistory
        {
            BookingId = bookingId,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            ChangedAt = DateTimeOffset.UtcNow,
            Reason = reason
        });

        await Task.CompletedTask;
    }

    public async Task ReplaceUpgradesAsync(
        Guid bookingId,
        IReadOnlyList<CreateBookingUpgradeModel> upgrades,
        CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.BookingUpgrades
            .Where(u => u.BookingId == bookingId)
            .ToListAsync(cancellationToken);

        _dbContext.BookingUpgrades.RemoveRange(existing);

        foreach (var upgrade in upgrades)
        {
            _dbContext.BookingUpgrades.Add(new BookingUpgrade
            {
                BookingId = bookingId,
                UpgradeOptionId = upgrade.UpgradeOptionId,
                Quantity = upgrade.Quantity,
                UnitPrice = upgrade.UnitPrice,
                TotalPrice = upgrade.TotalPrice,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }
    }

    private IQueryable<Booking> Query() =>
        _dbContext.Bookings
            .AsNoTracking()
            .Include(b => b.Property)
            .Include(b => b.Guests)
            .Include(b => b.Rooms)
            .ThenInclude(r => r.RoomType)
            .Include(b => b.Upgrades)
            .ThenInclude(u => u.UpgradeOption);

    private static BookingData Map(Booking booking) => new()
    {
        Id = booking.Id,
        BookingNumber = booking.BookingNumber,
        UserId = booking.UserId,
        PropertyId = booking.PropertyId,
        PropertyName = booking.Property?.Name ?? string.Empty,
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
        Guests = booking.Guests
            .OrderBy(g => g.SortOrder)
            .Select(g => new BookingGuestData
            {
                Id = g.Id,
                FirstName = g.FirstName,
                LastName = g.LastName,
                Email = g.Email,
                PhoneNumber = g.PhoneNumber,
                IsPrimary = g.IsPrimary,
                SortOrder = g.SortOrder
            })
            .ToList(),
        Rooms = booking.Rooms.Select(r => new BookingRoomData
        {
            Id = r.Id,
            RoomTypeId = r.RoomTypeId,
            RoomTypeName = r.RoomType?.Name ?? string.Empty,
            Quantity = r.Quantity,
            PricePerNight = r.PricePerNight,
            Nights = r.Nights,
            SubTotal = r.SubTotal
        }).ToList(),
        Upgrades = booking.Upgrades.Select(u => new BookingUpgradeData
        {
            Id = u.Id,
            UpgradeOptionId = u.UpgradeOptionId,
            Name = u.UpgradeOption?.Name ?? string.Empty,
            Quantity = u.Quantity,
            UnitPrice = u.UnitPrice,
            TotalPrice = u.TotalPrice,
            IsMandatory = u.UpgradeOption?.IsMandatory ?? false
        }).ToList()
    };
}
