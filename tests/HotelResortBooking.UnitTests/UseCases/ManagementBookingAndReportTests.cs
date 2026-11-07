using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Features.Management.Reports;
using HotelResortBooking.Application.UseCases.Bookings;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.UnitTests.Fakes;
using Xunit;

namespace HotelResortBooking.UnitTests.UseCases;

public sealed class ManagementBookingAndReportTests
{
    [Fact]
    public async Task ManagementCancel_UsesSameDomainRules_WithoutOwnership()
    {
        var access = new FakeAccessContext();
        access.Authenticate(Guid.NewGuid());
        var bookings = new FakeBookingRepository();
        var inventory = new FakeRoomInventoryRepository();
        var loyalty = new FakeLoyaltyRepository();
        var clock = new FakeDateTimeProvider { UtcNow = new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero) };
        var ownerId = Guid.NewGuid();
        var roomTypeId = Guid.NewGuid();
        var booking = new BookingData
        {
            Id = Guid.NewGuid(),
            BookingNumber = "BK-M",
            UserId = ownerId,
            PropertyId = Guid.NewGuid(),
            PropertyName = "Hotel",
            CheckInDate = new DateOnly(2026, 8, 20),
            CheckOutDate = new DateOnly(2026, 8, 22),
            Adults = 2,
            RoomsCount = 1,
            Status = BookingStatus.Pending,
            Currency = "USD",
            SubTotal = 200m,
            TotalAmount = 220m,
            CancellationDeadline = clock.UtcNow.AddDays(2),
            CreatedAt = clock.UtcNow,
            Rooms =
            [
                new BookingRoomData
                {
                    Id = Guid.NewGuid(),
                    RoomTypeId = roomTypeId,
                    RoomTypeName = "Deluxe",
                    Quantity = 1,
                    PricePerNight = 100m,
                    Nights = 2,
                    SubTotal = 200m
                }
            ]
        };
        bookings.Seed(booking);
        inventory.Seed(roomTypeId, booking.CheckInDate, booking.CheckOutDate, 0);

        var sut = new CancelBookingUseCase(
            access,
            bookings,
            new HotelResortBooking.Application.Services.RoomAvailabilityService(inventory),
            loyalty,
            clock,
            new FakeUnitOfWork());

        var denied = await sut.ExecuteAsync(new CancelBookingRequest { BookingId = booking.Id });
        Assert.True(denied.IsFailure);

        var allowed = await sut.ExecuteAsync(new CancelBookingRequest
        {
            BookingId = booking.Id,
            BypassOwnershipCheck = true,
            Reason = "Cancelled by management"
        });
        Assert.True(allowed.IsSuccess);
        Assert.Equal(BookingStatus.Cancelled, allowed.Value.Booking.Status);
    }

    [Fact]
    public async Task Confirm_ChangesPendingToConfirmed()
    {
        var access = new FakeAccessContext();
        access.Authenticate(Guid.NewGuid());
        var bookings = new FakeBookingRepository();
        var clock = new FakeDateTimeProvider { UtcNow = new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero) };
        var bookingId = Guid.NewGuid();
        bookings.Seed(new BookingData
        {
            Id = bookingId,
            BookingNumber = "BK-P",
            UserId = Guid.NewGuid(),
            PropertyId = Guid.NewGuid(),
            PropertyName = "Hotel",
            CheckInDate = new DateOnly(2026, 8, 20),
            CheckOutDate = new DateOnly(2026, 8, 22),
            Adults = 2,
            RoomsCount = 1,
            Status = BookingStatus.Pending,
            Currency = "USD",
            TotalAmount = 220m,
            CreatedAt = clock.UtcNow
        });

        var sut = new ConfirmBookingUseCase(access, bookings, clock, new FakeUnitOfWork());

        var denied = await sut.ExecuteAsync(new ConfirmBookingRequest { BookingId = bookingId });
        Assert.True(denied.IsFailure);

        var confirmed = await sut.ExecuteAsync(new ConfirmBookingRequest
        {
            BookingId = bookingId,
            BypassOwnershipCheck = true
        });
        Assert.True(confirmed.IsSuccess);
        Assert.Equal(BookingStatus.Confirmed, confirmed.Value.Booking.Status);
        Assert.Equal(clock.UtcNow, confirmed.Value.Booking.ConfirmedAt);

        var alreadyConfirmed = await sut.ExecuteAsync(new ConfirmBookingRequest
        {
            BookingId = bookingId,
            BypassOwnershipCheck = true
        });
        Assert.True(alreadyConfirmed.IsFailure);
        Assert.Equal("BookingNotConfirmable", alreadyConfirmed.Error!.Code);
    }

    [Fact]
    public async Task Complete_ChangesConfirmedToCompleted()
    {
        var access = new FakeAccessContext();
        access.Authenticate(Guid.NewGuid());
        var bookings = new FakeBookingRepository();
        var bookingId = Guid.NewGuid();
        var confirmedAt = new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero);
        bookings.Seed(new BookingData
        {
            Id = bookingId,
            BookingNumber = "BK-C",
            UserId = Guid.NewGuid(),
            PropertyId = Guid.NewGuid(),
            PropertyName = "Hotel",
            CheckInDate = new DateOnly(2026, 8, 20),
            CheckOutDate = new DateOnly(2026, 8, 22),
            Adults = 2,
            RoomsCount = 1,
            Status = BookingStatus.Confirmed,
            Currency = "USD",
            TotalAmount = 220m,
            ConfirmedAt = confirmedAt,
            CreatedAt = confirmedAt
        });

        var pendingId = Guid.NewGuid();
        bookings.Seed(new BookingData
        {
            Id = pendingId,
            BookingNumber = "BK-P",
            UserId = Guid.NewGuid(),
            PropertyId = Guid.NewGuid(),
            PropertyName = "Hotel",
            CheckInDate = new DateOnly(2026, 8, 20),
            CheckOutDate = new DateOnly(2026, 8, 22),
            Adults = 2,
            RoomsCount = 1,
            Status = BookingStatus.Pending,
            Currency = "USD",
            TotalAmount = 220m,
            CreatedAt = confirmedAt
        });

        var sut = new CompleteBookingUseCase(access, bookings, new FakeUnitOfWork());

        var denied = await sut.ExecuteAsync(new CompleteBookingRequest { BookingId = bookingId });
        Assert.True(denied.IsFailure);

        var pendingDenied = await sut.ExecuteAsync(new CompleteBookingRequest
        {
            BookingId = pendingId,
            BypassOwnershipCheck = true
        });
        Assert.True(pendingDenied.IsFailure);
        Assert.Equal("BookingNotCompletable", pendingDenied.Error!.Code);

        var completed = await sut.ExecuteAsync(new CompleteBookingRequest
        {
            BookingId = bookingId,
            BypassOwnershipCheck = true
        });
        Assert.True(completed.IsSuccess);
        Assert.Equal(BookingStatus.Completed, completed.Value.Booking.Status);
        Assert.Equal(confirmedAt, completed.Value.Booking.ConfirmedAt);

        var alreadyCompleted = await sut.ExecuteAsync(new CompleteBookingRequest
        {
            BookingId = bookingId,
            BypassOwnershipCheck = true
        });
        Assert.True(alreadyCompleted.IsFailure);
        Assert.Equal("BookingNotCompletable", alreadyCompleted.Error!.Code);
    }

    [Fact]
    public async Task Reports_ReturnRepositorySummaries()
    {
        var queries = new FakeManagementQueryRepository();
        var sut = new ManagementReportHandlers(queries, new FakeDateTimeProvider());
        var result = await sut.GetBookingsAsync(new ReportFilter());
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
    }
}
