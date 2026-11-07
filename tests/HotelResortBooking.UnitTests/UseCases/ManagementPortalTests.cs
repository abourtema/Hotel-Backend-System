using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Paging;
using HotelResortBooking.Application.Features.Management.Dashboard;
using HotelResortBooking.Application.Features.Management.Hotels;
using HotelResortBooking.Application.Features.Management.Loyalty;
using HotelResortBooking.Application.Features.Management.Payments;
using HotelResortBooking.Application.Features.Management.Reviews;
using HotelResortBooking.Application.Features.Management.Rooms;
using HotelResortBooking.Application.Features.Management.Users;
using HotelResortBooking.Application.UseCases.Payments;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.Domain.Management;
using HotelResortBooking.UnitTests.Fakes;
using Xunit;

namespace HotelResortBooking.UnitTests.UseCases;

public sealed class ManagementPortalTests
{
    [Fact]
    public async Task CreateHotel_PersistsAndAudits()
    {
        var catalog = new FakeManagementCatalogRepository();
        var audit = new FakeAuditLogger();
        var uow = new FakeUnitOfWork();
        var sut = new CreateHotelHandler(catalog, audit, uow);

        var result = await sut.ExecuteAsync(new SaveHotelRequest
        {
            Name = "Seaside",
            Description = "Resort",
            City = "Tripoli",
            Country = "Libya",
            Address = "1 Harbor",
            CheckInTime = new TimeOnly(15, 0),
            CheckOutTime = new TimeOnly(11, 0)
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("Seaside", result.Value.Name);
        Assert.Contains(audit.Entries, e => e.Action == AuditActions.HotelCreated);
        Assert.Equal(2, uow.SaveChangesCallCount);
    }

    [Fact]
    public async Task InventoryUpdate_RejectsAvailableGreaterThanTotal()
    {
        var catalog = new FakeManagementCatalogRepository();
        catalog.Rooms.Add(new ManagedRoomDto { Id = Guid.NewGuid(), Name = "Deluxe", PropertyId = Guid.NewGuid() });
        var roomId = catalog.Rooms[0].Id;
        var sut = new RoomManagementHandlers(catalog, new FakeImageUrlResolver(), new FakeAuditLogger(), new FakeUnitOfWork());

        var result = await sut.UpdateAvailabilityAsync(roomId,
        [
            new RoomInventoryUpdateItem { Date = new DateOnly(2026, 9, 1), TotalRooms = 2, AvailableRooms = 5 }
        ]);

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidInventory", result.Error!.Code);
    }

    [Fact]
    public async Task LoyaltyAdjust_CreatesAuditableTransaction()
    {
        var loyalty = new FakeLoyaltyRepository();
        var users = new FakeUserRepository();
        var user = await users.CreateAsync("a@b.com", "hash", "A", "B", null);
        loyalty.SeedBalance(user.Id, 10);
        var audit = new FakeAuditLogger();
        var sut = new LoyaltyManagementHandlers(loyalty, users, audit, new FakeUnitOfWork());

        var result = await sut.AdjustAsync(user.Id, new AdjustLoyaltyRequest { PointsDelta = 5, Reason = "Bonus" });

        Assert.True(result.IsSuccess);
        Assert.Equal(15, result.Value.CurrentBalance);
        var tx = await loyalty.GetTransactionsAsync(user.Id);
        Assert.Contains(tx, t => t.TransactionType == LoyaltyTransactionType.Adjustment && t.Points == 5);
        Assert.Contains(audit.Entries, e => e.Action == AuditActions.LoyaltyPointsAdjusted);
    }

    [Fact]
    public async Task ReviewModeration_IsAudited()
    {
        var queries = new FakeManagementQueryRepository();
        var reviewId = Guid.NewGuid();
        queries.Reviews.Add(new ManagedReviewListItem
        {
            Id = reviewId,
            Status = ReviewStatus.Pending,
            Rating = 5
        });
        var audit = new FakeAuditLogger();
        var sut = new ReviewManagementHandlers(queries, audit, new FakeUnitOfWork());

        var result = await sut.ModerateAsync(reviewId, ReviewStatus.Published);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReviewStatus.Published, queries.Reviews[0].Status);
        Assert.Contains(audit.Entries, e => e.Action == AuditActions.ReviewApproved);
    }

    [Fact]
    public async Task Dashboard_UsesRepositoryTotals()
    {
        var queries = new FakeManagementQueryRepository
        {
            Dashboard = new ManagementDashboardTotals
            {
                TotalHotels = 4,
                ActiveHotels = 3,
                TotalBookings = 10,
                TotalRevenue = 250m
            }
        };
        var sut = new GetManagementDashboardHandler(queries);

        var result = await sut.ExecuteAsync(new GetManagementDashboardQuery());

        Assert.Equal(4, result.TotalHotels);
        Assert.Equal(3, result.ActiveHotels);
        Assert.Equal(250m, result.TotalRevenue);
    }

    [Fact]
    public async Task PaymentRefund_RequiresSucceededPayment()
    {
        var payments = new FakePaymentRepository();
        var payment = await payments.AddAsync(new CreatePaymentModel
        {
            BookingId = Guid.NewGuid(),
            Amount = 100,
            Currency = "USD",
            PaymentMethod = PaymentMethodType.CreditCard
        });
        var sut = new PaymentManagementHandlers(
            new FakeManagementQueryRepository(),
            payments,
            new ConfirmPayAtHotelPaymentUseCase(
                payments,
                new FakeBookingRepository(),
                new FakeDateTimeProvider(),
                new FakeUnitOfWork()),
            new FakePaymentGateway(),
            new FakeDateTimeProvider(),
            new FakeAuditLogger(),
            new FakeUnitOfWork());

        var result = await sut.RefundAsync(payment.Id);
        Assert.True(result.IsFailure);
        Assert.Equal("PaymentNotRefundable", result.Error!.Code);
    }

    [Fact]
    public async Task UserRoleAssignment_IsAudited()
    {
        var users = new FakeManagementUserRepository();
        var accounts = new FakeUserRepository();
        var created = await users.CreateAsync(new CreateManagementUserModel
        {
            Email = "mgr@example.com",
            PasswordHash = "hash",
            FirstName = "Pat",
            LastName = "Lee"
        });
        var audit = new FakeAuditLogger();
        var sut = new ManagementUserHandlers(users, accounts, new FakePasswordHasher(), audit, new FakeUnitOfWork());
        var roleId = Guid.NewGuid();

        var result = await sut.AssignRoleAsync(created.Id, roleId);

        Assert.True(result.IsSuccess);
        Assert.Contains(audit.Entries, e => e.Action == AuditActions.RoleAssigned);
        Assert.Contains(users.AssignedRoles, r => r.UserId == created.Id && r.RoleId == roleId);
    }
}

public sealed class FakeAuditLogger : IAuditLogger
{
    public List<AuditLogEntry> Entries { get; } = [];

    public Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        Entries.Add(entry);
        return Task.CompletedTask;
    }
}

public sealed class FakeManagementCatalogRepository : IManagementCatalogRepository
{
    public List<ManagedHotelDetails> Hotels { get; } = [];
    public List<ManagedRoomDto> Rooms { get; } = [];

    public Task<PagedResult<ManagedHotelListItem>> SearchHotelsAsync(string? search, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default)
        => Task.FromResult(new PagedResult<ManagedHotelListItem> { Page = page, PageSize = pageSize });

    public Task<ManagedHotelDetails?> GetHotelAsync(Guid hotelId, CancellationToken cancellationToken = default)
        => Task.FromResult(Hotels.FirstOrDefault(h => h.Id == hotelId));

    public Task<ManagedHotelDetails> CreateHotelAsync(SaveHotelModel model, CancellationToken cancellationToken = default)
    {
        var hotel = new ManagedHotelDetails
        {
            Id = Guid.NewGuid(),
            Name = model.Name,
            Description = model.Description,
            City = model.City,
            Country = model.Country,
            IsActive = model.IsActive
        };
        Hotels.Add(hotel);
        return Task.FromResult(hotel);
    }

    public Task<ManagedHotelDetails?> UpdateHotelAsync(Guid hotelId, SaveHotelModel model, CancellationToken cancellationToken = default)
        => Task.FromResult<ManagedHotelDetails?>(null);

    public Task<bool> SetHotelActiveAsync(Guid hotelId, bool isActive, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<ManagedImageDto> AddHotelImageAsync(Guid hotelId, string imageUrl, string? altText, int? sortOrder, bool isPrimary, CancellationToken cancellationToken = default)
    {
        var image = new ManagedImageDto
        {
            Id = Guid.NewGuid(),
            ImageUrl = imageUrl,
            AltText = altText,
            SortOrder = sortOrder ?? 0,
            IsPrimary = isPrimary
        };
        var hotel = Hotels.FirstOrDefault(h => h.Id == hotelId);
        if (hotel is not null)
        {
            var images = hotel.Images.ToList();
            images.Add(image);
            var index = Hotels.FindIndex(h => h.Id == hotelId);
            Hotels[index] = CloneHotel(hotel, images);
        }

        return Task.FromResult(image);
    }

    public Task<ManagedImageDto?> UpdateHotelImageAsync(Guid hotelId, Guid imageId, string? imageUrl, string? altText, int? sortOrder, bool? isPrimary, CancellationToken cancellationToken = default)
        => Task.FromResult<ManagedImageDto?>(null);

    public Task<string?> DeleteHotelImageAsync(Guid hotelId, Guid imageId, CancellationToken cancellationToken = default)
    {
        var hotel = Hotels.FirstOrDefault(h => h.Id == hotelId);
        var image = hotel?.Images.FirstOrDefault(i => i.Id == imageId);
        if (hotel is null || image is null)
        {
            return Task.FromResult<string?>(null);
        }

        var images = hotel.Images.Where(i => i.Id != imageId).ToList();
        var index = Hotels.FindIndex(h => h.Id == hotelId);
        Hotels[index] = CloneHotel(hotel, images);
        return Task.FromResult<string?>(image.ImageUrl);
    }

    private static ManagedHotelDetails CloneHotel(ManagedHotelDetails hotel, IReadOnlyList<ManagedImageDto> images)
        => new()
        {
            Id = hotel.Id,
            Name = hotel.Name,
            Description = hotel.Description,
            City = hotel.City,
            Country = hotel.Country,
            IsActive = hotel.IsActive,
            Images = images,
            Amenities = hotel.Amenities
        };

    public Task<bool> SetPrimaryHotelImageAsync(Guid hotelId, Guid imageId, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task ReorderHotelImagesAsync(Guid hotelId, IReadOnlyList<Guid> imageIdsInOrder, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<ManagedImageDto> AddRoomImageAsync(Guid roomTypeId, string imageUrl, string? altText, int? sortOrder, bool isPrimary, CancellationToken cancellationToken = default)
    {
        var image = new ManagedImageDto
        {
            Id = Guid.NewGuid(),
            ImageUrl = imageUrl,
            AltText = altText,
            SortOrder = sortOrder ?? 0,
            IsPrimary = isPrimary
        };
        var room = Rooms.FirstOrDefault(r => r.Id == roomTypeId);
        if (room is not null)
        {
            var images = room.Images.ToList();
            images.Add(image);
            var index = Rooms.FindIndex(r => r.Id == roomTypeId);
            Rooms[index] = CloneRoom(room, images);
        }

        return Task.FromResult(image);
    }

    public Task<ManagedImageDto?> UpdateRoomImageAsync(Guid roomTypeId, Guid imageId, string? imageUrl, string? altText, int? sortOrder, bool? isPrimary, CancellationToken cancellationToken = default)
        => Task.FromResult<ManagedImageDto?>(null);

    public Task<string?> DeleteRoomImageAsync(Guid roomTypeId, Guid imageId, CancellationToken cancellationToken = default)
    {
        var room = Rooms.FirstOrDefault(r => r.Id == roomTypeId);
        var image = room?.Images.FirstOrDefault(i => i.Id == imageId);
        if (room is null || image is null)
        {
            return Task.FromResult<string?>(null);
        }

        var images = room.Images.Where(i => i.Id != imageId).ToList();
        var index = Rooms.FindIndex(r => r.Id == roomTypeId);
        Rooms[index] = CloneRoom(room, images);
        return Task.FromResult<string?>(image.ImageUrl);
    }

    public Task<bool> SetPrimaryRoomImageAsync(Guid roomTypeId, Guid imageId, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task ReorderRoomImagesAsync(Guid roomTypeId, IReadOnlyList<Guid> imageIdsInOrder, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    private static ManagedRoomDto CloneRoom(ManagedRoomDto room, IReadOnlyList<ManagedImageDto> images)
        => new()
        {
            Id = room.Id,
            PropertyId = room.PropertyId,
            PropertyName = room.PropertyName,
            Name = room.Name,
            Description = room.Description,
            RoomSize = room.RoomSize,
            MaxAdults = room.MaxAdults,
            MaxChildren = room.MaxChildren,
            MaxGuests = room.MaxGuests,
            BedType = room.BedType,
            BasePricePerNight = room.BasePricePerNight,
            Currency = room.Currency,
            IsActive = room.IsActive,
            Images = images
        };

    public Task<IReadOnlyList<ManagedAmenityDto>> GetAmenitiesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ManagedAmenityDto>>([]);

    public Task<ManagedAmenityDto> CreateAmenityAsync(string name, string? description, string? icon, CancellationToken cancellationToken = default)
        => Task.FromResult(new ManagedAmenityDto { Id = Guid.NewGuid(), Name = name });

    public Task<ManagedAmenityDto?> UpdateAmenityAsync(Guid id, string name, string? description, string? icon, CancellationToken cancellationToken = default)
        => Task.FromResult<ManagedAmenityDto?>(null);

    public Task<bool> SetAmenityActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task AssignAmenityToPropertyAsync(Guid propertyId, Guid amenityId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RemoveAmenityFromPropertyAsync(Guid propertyId, Guid amenityId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<PagedResult<ManagedRoomDto>> SearchRoomsAsync(Guid? propertyId, bool? isActive, int page, int pageSize, CancellationToken cancellationToken = default)
        => Task.FromResult(new PagedResult<ManagedRoomDto> { Page = page, PageSize = pageSize });

    public Task<ManagedRoomDto?> GetRoomAsync(Guid roomTypeId, CancellationToken cancellationToken = default)
        => Task.FromResult(Rooms.FirstOrDefault(r => r.Id == roomTypeId));

    public Task<ManagedRoomDto> CreateRoomAsync(SaveRoomTypeModel model, CancellationToken cancellationToken = default)
        => Task.FromResult(new ManagedRoomDto { Id = Guid.NewGuid(), Name = model.Name, BasePricePerNight = model.BasePricePerNight });

    public Task<ManagedRoomDto?> UpdateRoomAsync(Guid roomTypeId, SaveRoomTypeModel model, CancellationToken cancellationToken = default)
        => Task.FromResult<ManagedRoomDto?>(null);

    public Task<bool> SetRoomActiveAsync(Guid roomTypeId, bool isActive, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<IReadOnlyList<RoomInventoryDto>> GetInventoryAsync(Guid roomTypeId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<RoomInventoryDto>>([]);

    public Task UpsertInventoryAsync(Guid roomTypeId, IReadOnlyList<RoomInventoryUpdateItem> items, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<ManagedUpgradeDto>> GetUpgradesAsync(Guid? propertyId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ManagedUpgradeDto>>([]);

    public Task<ManagedUpgradeDto?> GetUpgradeAsync(Guid upgradeId, CancellationToken cancellationToken = default)
        => Task.FromResult<ManagedUpgradeDto?>(null);

    public Task<ManagedUpgradeDto> CreateUpgradeAsync(SaveUpgradeModel model, CancellationToken cancellationToken = default)
        => Task.FromResult(new ManagedUpgradeDto { Id = Guid.NewGuid(), Name = model.Name });

    public Task<ManagedUpgradeDto?> UpdateUpgradeAsync(Guid upgradeId, SaveUpgradeModel model, CancellationToken cancellationToken = default)
        => Task.FromResult<ManagedUpgradeDto?>(null);

    public Task<bool> DeleteUpgradeAsync(Guid upgradeId, CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}

public sealed class FakeManagementQueryRepository : IManagementQueryRepository
{
    public ManagementDashboardTotals Dashboard { get; set; } = new();

    public List<ManagedReviewListItem> Reviews { get; } = [];

    public Task<ManagementDashboardTotals> GetDashboardAsync(DateTimeOffset? dateFrom, DateTimeOffset? dateTo, CancellationToken cancellationToken = default)
        => Task.FromResult(Dashboard);

    public Task<PagedResult<ManagedBookingListItem>> SearchBookingsAsync(string? bookingNumber, Guid? userId, Guid? propertyId, BookingStatus? status, DateOnly? checkInDate, DateOnly? checkOutDate, PaymentStatus? paymentStatus, PaymentMethodType? paymentMethod, int page, int pageSize, CancellationToken cancellationToken = default)
        => Task.FromResult(new PagedResult<ManagedBookingListItem> { Page = page, PageSize = pageSize });

    public Task<PagedResult<ManagedCustomerListItem>> SearchCustomersAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default)
        => Task.FromResult(new PagedResult<ManagedCustomerListItem> { Page = page, PageSize = pageSize });

    public Task<ManagedCustomerListItem?> GetCustomerAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult<ManagedCustomerListItem?>(null);

    public Task<PagedResult<ManagedPaymentListItem>> SearchPaymentsAsync(Guid? bookingId, Guid? userId, PaymentStatus? status, PaymentMethodType? paymentMethod, DateTimeOffset? dateFrom, DateTimeOffset? dateTo, int page, int pageSize, CancellationToken cancellationToken = default)
        => Task.FromResult(new PagedResult<ManagedPaymentListItem> { Page = page, PageSize = pageSize });

    public Task<PagedResult<ManagedReviewListItem>> SearchReviewsAsync(ReviewStatus? status, Guid? propertyId, int page, int pageSize, CancellationToken cancellationToken = default)
        => Task.FromResult(new PagedResult<ManagedReviewListItem>
        {
            Items = Reviews,
            Page = page,
            PageSize = pageSize,
            TotalCount = Reviews.Count
        });

    public Task<ManagedReviewListItem?> GetReviewAsync(Guid reviewId, CancellationToken cancellationToken = default)
        => Task.FromResult(Reviews.FirstOrDefault(r => r.Id == reviewId));

    public Task<bool> SetReviewStatusAsync(Guid reviewId, ReviewStatus status, CancellationToken cancellationToken = default)
    {
        var index = Reviews.FindIndex(r => r.Id == reviewId);
        if (index < 0)
        {
            return Task.FromResult(false);
        }

        Reviews[index] = new ManagedReviewListItem
        {
            Id = Reviews[index].Id,
            UserId = Reviews[index].UserId,
            PropertyId = Reviews[index].PropertyId,
            BookingId = Reviews[index].BookingId,
            Rating = Reviews[index].Rating,
            Comment = Reviews[index].Comment,
            Status = status,
            CreatedAt = Reviews[index].CreatedAt
        };
        return Task.FromResult(true);
    }

    public Task<bool> SetCustomerActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task<IReadOnlyList<BookingsReportRow>> GetBookingsReportAsync(DateTimeOffset? dateFrom, DateTimeOffset? dateTo, Guid? propertyId, BookingStatus? status, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<BookingsReportRow>>([]);

    public Task<IReadOnlyList<RevenueReportRow>> GetRevenueReportAsync(DateTimeOffset? dateFrom, DateTimeOffset? dateTo, Guid? propertyId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<RevenueReportRow>>([]);

    public Task<IReadOnlyList<OccupancyReportRow>> GetOccupancyReportAsync(DateOnly from, DateOnly to, Guid? propertyId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<OccupancyReportRow>>([]);

    public Task<IReadOnlyList<PaymentsReportRow>> GetPaymentsReportAsync(DateTimeOffset? dateFrom, DateTimeOffset? dateTo, Guid? propertyId, PaymentStatus? status, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<PaymentsReportRow>>([]);
}

public sealed class FakeManagementUserRepository : IManagementUserRepository
{
    public List<ManagementUserListItem> Users { get; } = [];
    public List<(Guid UserId, Guid RoleId)> AssignedRoles { get; } = [];

    public Task<PagedResult<ManagementUserListItem>> SearchAsync(string? search, int page, int pageSize, CancellationToken cancellationToken = default)
        => Task.FromResult(new PagedResult<ManagementUserListItem> { Items = Users, Page = page, PageSize = pageSize, TotalCount = Users.Count });

    public Task<ManagementUserListItem?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(Users.FirstOrDefault(u => u.Id == userId));

    public Task<ManagementUserListItem> CreateAsync(CreateManagementUserModel model, CancellationToken cancellationToken = default)
    {
        var user = new ManagementUserListItem
        {
            Id = Guid.NewGuid(),
            Email = model.Email,
            FirstName = model.FirstName,
            LastName = model.LastName,
            PhoneNumber = model.PhoneNumber,
            IsActive = true,
            PrimaryRole = "HotelManager",
            CreatedAt = DateTimeOffset.UtcNow
        };
        Users.Add(user);
        return Task.FromResult(user);
    }

    public Task UpdateAsync(Guid userId, string firstName, string lastName, string? phoneNumber, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task AssignRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        AssignedRoles.Add((userId, roleId));
        return Task.CompletedTask;
    }

    public Task RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
