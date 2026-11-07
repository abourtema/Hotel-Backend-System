using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Paging;
using HotelResortBooking.Application.Security;
using HotelResortBooking.Domain.Common;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelResortBooking.Infrastructure.Repositories;

public sealed class ManagementQueryRepository : IManagementQueryRepository
{
    private readonly ApplicationDbContext _db;

    public ManagementQueryRepository(ApplicationDbContext db) => _db = db;

    public async Task<ManagementDashboardTotals> GetDashboardAsync(
        DateTimeOffset? dateFrom,
        DateTimeOffset? dateTo,
        CancellationToken cancellationToken = default)
    {
        dateFrom = dateFrom.ToUtc();
        dateTo = dateTo.ToUtc();

        var bookings = _db.Bookings.AsNoTracking().AsQueryable();
        var payments = _db.Payments.AsNoTracking().AsQueryable();
        var reviews = _db.Reviews.AsNoTracking().AsQueryable();

        if (dateFrom is DateTimeOffset from)
        {
            bookings = bookings.Where(b => b.CreatedAt >= from);
            payments = payments.Where(p => p.CreatedAt >= from);
            reviews = reviews.Where(r => r.CreatedAt >= from);
        }

        if (dateTo is DateTimeOffset to)
        {
            bookings = bookings.Where(b => b.CreatedAt <= to);
            payments = payments.Where(p => p.CreatedAt <= to);
            reviews = reviews.Where(r => r.CreatedAt <= to);
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var totalHotels = await _db.Properties.AsNoTracking().CountAsync(cancellationToken);
        var activeHotels = await _db.Properties.AsNoTracking().CountAsync(p => p.IsActive, cancellationToken);
        var totalRooms = await _db.RoomTypes.AsNoTracking().CountAsync(cancellationToken);

        var availableRooms = await _db.RoomInventories.AsNoTracking()
            .Where(i => i.Date == today)
            .SumAsync(i => (int?)i.AvailableRooms, cancellationToken) ?? 0;

        if (availableRooms == 0)
        {
            availableRooms = await _db.RoomInventories.AsNoTracking()
                .SumAsync(i => (int?)i.AvailableRooms, cancellationToken) ?? 0;
        }

        var totalBookings = await bookings.CountAsync(cancellationToken);
        var pendingBookings = await bookings.CountAsync(b => b.Status == BookingStatus.Pending, cancellationToken);
        var confirmedBookings = await bookings.CountAsync(b => b.Status == BookingStatus.Confirmed, cancellationToken);
        var cancelledBookings = await bookings.CountAsync(b => b.Status == BookingStatus.Cancelled, cancellationToken);
        var completedBookings = await bookings.CountAsync(b => b.Status == BookingStatus.Completed, cancellationToken);

        var totalCustomers = await _db.Users.AsNoTracking()
            .CountAsync(u => u.Role == Roles.User && !u.UserRoles.Any(), cancellationToken);

        var totalRevenue = await payments
            .Where(p => p.Status == PaymentStatus.Succeeded)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var totalPayments = await payments.CountAsync(cancellationToken);
        var successfulPayments = await payments.CountAsync(p => p.Status == PaymentStatus.Succeeded, cancellationToken);
        var failedPayments = await payments.CountAsync(p => p.Status == PaymentStatus.Failed, cancellationToken);
        var currentLoyaltyPoints = await _db.LoyaltyAccounts.AsNoTracking()
            .SumAsync(a => (int?)a.CurrentBalance, cancellationToken) ?? 0;
        var reviewCount = await reviews.CountAsync(cancellationToken);

        return new ManagementDashboardTotals
        {
            TotalHotels = totalHotels,
            ActiveHotels = activeHotels,
            TotalRooms = totalRooms,
            AvailableRooms = availableRooms,
            TotalBookings = totalBookings,
            PendingBookings = pendingBookings,
            ConfirmedBookings = confirmedBookings,
            CancelledBookings = cancelledBookings,
            CompletedBookings = completedBookings,
            TotalCustomers = totalCustomers,
            TotalRevenue = totalRevenue,
            TotalPayments = totalPayments,
            SuccessfulPayments = successfulPayments,
            FailedPayments = failedPayments,
            CurrentLoyaltyPoints = currentLoyaltyPoints,
            ReviewCount = reviewCount
        };
    }

    public async Task<PagedResult<ManagedBookingListItem>> SearchBookingsAsync(
        string? bookingNumber,
        Guid? userId,
        Guid? propertyId,
        BookingStatus? status,
        DateOnly? checkInDate,
        DateOnly? checkOutDate,
        PaymentStatus? paymentStatus,
        PaymentMethodType? paymentMethod,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Bookings.AsNoTracking().Include(b => b.Property).AsQueryable();

        if (!string.IsNullOrWhiteSpace(bookingNumber))
        {
            var number = bookingNumber.Trim();
            query = query.Where(b => b.BookingNumber == number);
        }

        if (userId is Guid uid)
        {
            query = query.Where(b => b.UserId == uid);
        }

        if (propertyId is Guid pid)
        {
            query = query.Where(b => b.PropertyId == pid);
        }

        if (status is BookingStatus bookingStatus)
        {
            query = query.Where(b => b.Status == bookingStatus);
        }

        if (checkInDate is DateOnly checkIn)
        {
            query = query.Where(b => b.CheckInDate == checkIn);
        }

        if (checkOutDate is DateOnly checkOut)
        {
            query = query.Where(b => b.CheckOutDate == checkOut);
        }

        if (paymentStatus is PaymentStatus payStatus)
        {
            query = query.Where(b => b.Payments.Any(p => p.Status == payStatus));
        }

        if (paymentMethod is PaymentMethodType method)
        {
            query = query.Where(b => b.Payments.Any(p => p.PaymentMethod == method));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(b => new ManagedBookingListItem
            {
                Id = b.Id,
                BookingNumber = b.BookingNumber,
                UserId = b.UserId,
                PropertyId = b.PropertyId,
                PropertyName = b.Property.Name,
                CheckInDate = b.CheckInDate,
                CheckOutDate = b.CheckOutDate,
                Status = b.Status,
                Adults = b.Adults,
                Children = b.Children,
                RoomsCount = b.RoomsCount,
                SpecialRequests = b.SpecialRequests,
                GuestFirstName = b.Guests
                    .Where(g => g.IsPrimary)
                    .OrderBy(g => g.SortOrder)
                    .Select(g => g.FirstName)
                    .FirstOrDefault(),
                GuestLastName = b.Guests
                    .Where(g => g.IsPrimary)
                    .OrderBy(g => g.SortOrder)
                    .Select(g => g.LastName)
                    .FirstOrDefault(),
                GuestEmail = b.Guests
                    .Where(g => g.IsPrimary)
                    .OrderBy(g => g.SortOrder)
                    .Select(g => g.Email)
                    .FirstOrDefault(),
                GuestPhoneNumber = b.Guests
                    .Where(g => g.IsPrimary)
                    .OrderBy(g => g.SortOrder)
                    .Select(g => g.PhoneNumber)
                    .FirstOrDefault(),
                PaymentStatus = b.Payments
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => (PaymentStatus?)p.Status)
                    .FirstOrDefault(),
                PaymentMethod = b.Payments
                    .OrderByDescending(p => p.CreatedAt)
                    .Select(p => (PaymentMethodType?)p.PaymentMethod)
                    .FirstOrDefault(),
                TotalAmount = b.TotalAmount,
                Currency = b.Currency,
                CreatedAt = b.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ManagedBookingListItem>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<PagedResult<ManagedCustomerListItem>> SearchCustomersAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Users.AsNoTracking()
            .Where(u => u.Role == Roles.User && !u.UserRoles.Any());

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(u =>
                u.Email.ToLower().Contains(term)
                || u.FirstName.ToLower().Contains(term)
                || u.LastName.ToLower().Contains(term)
                || (u.PhoneNumber != null && u.PhoneNumber.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new ManagedCustomerListItem
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                PhoneNumber = u.PhoneNumber,
                IsActive = u.IsActive,
                BookingCount = u.Bookings.Count,
                CompletedBookingCount = u.Bookings.Count(b => b.Status == BookingStatus.Completed),
                LoyaltyBalance = u.LoyaltyAccount != null ? u.LoyaltyAccount.CurrentBalance : 0,
                CreatedAt = u.CreatedAt,
                City = u.Profile != null ? u.Profile.City : null,
                Country = u.Profile != null ? u.Profile.Country : null
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ManagedCustomerListItem>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<ManagedCustomerListItem?> GetCustomerAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new ManagedCustomerListItem
            {
                Id = u.Id,
                Email = u.Email,
                FirstName = u.FirstName,
                LastName = u.LastName,
                PhoneNumber = u.PhoneNumber,
                IsActive = u.IsActive,
                BookingCount = u.Bookings.Count,
                CompletedBookingCount = u.Bookings.Count(b => b.Status == BookingStatus.Completed),
                LoyaltyBalance = u.LoyaltyAccount != null ? u.LoyaltyAccount.CurrentBalance : 0,
                CreatedAt = u.CreatedAt,
                City = u.Profile != null ? u.Profile.City : null,
                Country = u.Profile != null ? u.Profile.Country : null
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<PagedResult<ManagedPaymentListItem>> SearchPaymentsAsync(
        Guid? bookingId,
        Guid? userId,
        PaymentStatus? status,
        PaymentMethodType? paymentMethod,
        DateTimeOffset? dateFrom,
        DateTimeOffset? dateTo,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Payments.AsNoTracking().AsQueryable();
        if (bookingId is Guid bid)
        {
            query = query.Where(p => p.BookingId == bid);
        }

        if (userId is Guid uid)
        {
            query = query.Where(p => p.UserId == uid);
        }

        if (status is PaymentStatus paymentStatus)
        {
            query = query.Where(p => p.Status == paymentStatus);
        }

        if (paymentMethod is PaymentMethodType method)
        {
            query = query.Where(p => p.PaymentMethod == method);
        }

        dateFrom = dateFrom.ToUtc();
        dateTo = dateTo.ToUtc();

        if (dateFrom is DateTimeOffset from)
        {
            query = query.Where(p => p.CreatedAt >= from);
        }

        if (dateTo is DateTimeOffset to)
        {
            query = query.Where(p => p.CreatedAt <= to);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ManagedPaymentListItem
            {
                Id = p.Id,
                BookingId = p.BookingId,
                UserId = p.UserId,
                Amount = p.Amount,
                Currency = p.Currency,
                Status = p.Status,
                PaymentMethod = p.PaymentMethod,
                CardBrand = p.CardBrand,
                CardLast4 = p.CardLast4,
                ProviderTransactionId = p.ProviderTransactionId,
                CreatedAt = p.CreatedAt,
                PaidAt = p.PaidAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ManagedPaymentListItem>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<PagedResult<ManagedReviewListItem>> SearchReviewsAsync(
        ReviewStatus? status,
        Guid? propertyId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Reviews.AsNoTracking().AsQueryable();
        if (status is ReviewStatus reviewStatus)
        {
            query = query.Where(r => r.Status == reviewStatus);
        }

        if (propertyId is Guid pid)
        {
            query = query.Where(r => r.PropertyId == pid);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new ManagedReviewListItem
            {
                Id = r.Id,
                UserId = r.UserId,
                PropertyId = r.PropertyId,
                BookingId = r.BookingId,
                Rating = r.Rating,
                Comment = r.Comment,
                Status = r.Status,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new PagedResult<ManagedReviewListItem>
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<ManagedReviewListItem?> GetReviewAsync(Guid reviewId, CancellationToken cancellationToken = default)
    {
        return await _db.Reviews.AsNoTracking()
            .Where(r => r.Id == reviewId)
            .Select(r => new ManagedReviewListItem
            {
                Id = r.Id,
                UserId = r.UserId,
                PropertyId = r.PropertyId,
                BookingId = r.BookingId,
                Rating = r.Rating,
                Comment = r.Comment,
                Status = r.Status,
                CreatedAt = r.CreatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> SetReviewStatusAsync(Guid reviewId, ReviewStatus status, CancellationToken cancellationToken = default)
    {
        var review = await _db.Reviews.FirstOrDefaultAsync(r => r.Id == reviewId, cancellationToken);
        if (review is null)
        {
            return false;
        }

        review.Status = status;
        return true;
    }

    public async Task<bool> SetCustomerActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return false;
        }

        user.IsActive = isActive;
        return true;
    }

    public async Task<IReadOnlyList<BookingsReportRow>> GetBookingsReportAsync(
        DateTimeOffset? dateFrom,
        DateTimeOffset? dateTo,
        Guid? propertyId,
        BookingStatus? status,
        CancellationToken cancellationToken = default)
    {
        dateFrom = dateFrom.ToUtc();
        dateTo = dateTo.ToUtc();

        var query = _db.Bookings.AsNoTracking().AsQueryable();
        if (dateFrom is DateTimeOffset from)
        {
            query = query.Where(b => b.CreatedAt >= from);
        }

        if (dateTo is DateTimeOffset to)
        {
            query = query.Where(b => b.CreatedAt <= to);
        }

        if (propertyId is Guid pid)
        {
            query = query.Where(b => b.PropertyId == pid);
        }

        if (status is BookingStatus bookingStatus)
        {
            query = query.Where(b => b.Status == bookingStatus);
        }

        return await query
            .GroupBy(b => b.Status)
            .Select(g => new BookingsReportRow
            {
                Status = g.Key,
                Count = g.Count(),
                TotalAmount = g.Sum(x => x.TotalAmount)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RevenueReportRow>> GetRevenueReportAsync(
        DateTimeOffset? dateFrom,
        DateTimeOffset? dateTo,
        Guid? propertyId,
        CancellationToken cancellationToken = default)
    {
        dateFrom = dateFrom.ToUtc();
        dateTo = dateTo.ToUtc();

        var query = _db.Payments.AsNoTracking()
            .Where(p => p.Status == PaymentStatus.Succeeded);

        if (dateFrom is DateTimeOffset from)
        {
            query = query.Where(p => p.CreatedAt >= from);
        }

        if (dateTo is DateTimeOffset to)
        {
            query = query.Where(p => p.CreatedAt <= to);
        }

        if (propertyId is Guid pid)
        {
            query = query.Where(p => p.Booking.PropertyId == pid);
        }

        return await query
            .GroupBy(p => p.CreatedAt.Date)
            .Select(g => new RevenueReportRow
            {
                Date = DateOnly.FromDateTime(g.Key),
                Revenue = g.Sum(x => x.Amount),
                BookingCount = g.Select(x => x.BookingId).Distinct().Count()
            })
            .OrderBy(r => r.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OccupancyReportRow>> GetOccupancyReportAsync(
        DateOnly from,
        DateOnly to,
        Guid? propertyId,
        CancellationToken cancellationToken = default)
    {
        var query = _db.RoomInventories.AsNoTracking()
            .Where(i => i.Date >= from && i.Date <= to);

        if (propertyId is Guid pid)
        {
            query = query.Where(i => i.RoomType.PropertyId == pid);
        }

        return await query
            .GroupBy(i => i.Date)
            .Select(g => new OccupancyReportRow
            {
                Date = g.Key,
                TotalRooms = g.Sum(x => x.TotalRooms),
                OccupiedRooms = g.Sum(x => x.TotalRooms - x.AvailableRooms),
                OccupancyRate = g.Sum(x => x.TotalRooms) == 0
                    ? 0
                    : (decimal)g.Sum(x => x.TotalRooms - x.AvailableRooms) / g.Sum(x => x.TotalRooms)
            })
            .OrderBy(r => r.Date)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PaymentsReportRow>> GetPaymentsReportAsync(
        DateTimeOffset? dateFrom,
        DateTimeOffset? dateTo,
        Guid? propertyId,
        PaymentStatus? status,
        CancellationToken cancellationToken = default)
    {
        dateFrom = dateFrom.ToUtc();
        dateTo = dateTo.ToUtc();

        var query = _db.Payments.AsNoTracking().AsQueryable();
        if (dateFrom is DateTimeOffset from)
        {
            query = query.Where(p => p.CreatedAt >= from);
        }

        if (dateTo is DateTimeOffset to)
        {
            query = query.Where(p => p.CreatedAt <= to);
        }

        if (propertyId is Guid pid)
        {
            query = query.Where(p => p.Booking.PropertyId == pid);
        }

        if (status is PaymentStatus paymentStatus)
        {
            query = query.Where(p => p.Status == paymentStatus);
        }

        return await query
            .GroupBy(p => p.Status)
            .Select(g => new PaymentsReportRow
            {
                Status = g.Key,
                Count = g.Count(),
                Amount = g.Sum(x => x.Amount)
            })
            .ToListAsync(cancellationToken);
    }
}
