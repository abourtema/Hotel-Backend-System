using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Services;
using HotelResortBooking.Application.UseCases.Bookings;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.UnitTests.Fakes;
using Xunit;

namespace HotelResortBooking.UnitTests.UseCases;

public sealed class CancelBookingUseCaseTests
{
    private readonly FakeAccessContext _access = new();
    private readonly FakeBookingRepository _bookings = new();
    private readonly FakeRoomInventoryRepository _inventory = new();
    private readonly FakeLoyaltyRepository _loyalty = new();
    private readonly FakeDateTimeProvider _clock = new() { UtcNow = new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero) };
    private readonly FakeUnitOfWork _uow = new();
    private readonly CancelBookingUseCase _sut;

    private readonly Guid _userId = Guid.NewGuid();

    public CancelBookingUseCaseTests()
    {
        _sut = new CancelBookingUseCase(
            _access,
            _bookings,
            new RoomAvailabilityService(_inventory),
            _loyalty,
            _clock,
            _uow);
    }

    [Fact]
    public async Task CancelValidBooking_Succeeds()
    {
        _access.Authenticate(_userId);
        var roomTypeId = Guid.NewGuid();
        var booking = CreateBooking(_userId, BookingStatus.Pending, roomTypeId);
        _bookings.Seed(booking);
        _inventory.Seed(roomTypeId, booking.CheckInDate, booking.CheckOutDate, 0);

        var result = await _sut.ExecuteAsync(new CancelBookingRequest { BookingId = booking.Id, Reason = "Plans changed" });

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Cancelled, result.Value.Booking.Status);
        Assert.NotNull(result.Value.Booking.CancelledAt);
        Assert.Equal(1, _uow.SaveChangesCallCount);
    }

    [Fact]
    public async Task UnauthorizedBookingAccess_Fails()
    {
        _access.Authenticate(_userId);
        var booking = CreateBooking(Guid.NewGuid(), BookingStatus.Pending, Guid.NewGuid());
        _bookings.Seed(booking);

        var result = await _sut.ExecuteAsync(new CancelBookingRequest { BookingId = booking.Id });

        Assert.True(result.IsFailure);
        Assert.Equal("UnauthorizedBookingAccess", result.Error!.Code);
    }

    private BookingData CreateBooking(Guid userId, BookingStatus status, Guid roomTypeId) => new()
    {
        Id = Guid.NewGuid(),
        BookingNumber = "BK123",
        UserId = userId,
        PropertyId = Guid.NewGuid(),
        PropertyName = "Hotel",
        CheckInDate = new DateOnly(2026, 8, 20),
        CheckOutDate = new DateOnly(2026, 8, 22),
        Adults = 2,
        RoomsCount = 1,
        Status = status,
        Currency = "USD",
        SubTotal = 200m,
        TotalAmount = 220m,
        CancellationDeadline = _clock.UtcNow.AddDays(2),
        CreatedAt = _clock.UtcNow,
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
}
