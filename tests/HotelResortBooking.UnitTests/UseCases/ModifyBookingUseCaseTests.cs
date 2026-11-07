using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Services;
using HotelResortBooking.Application.UseCases.Bookings;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.UnitTests.Fakes;
using Xunit;

namespace HotelResortBooking.UnitTests.UseCases;

public sealed class ModifyBookingUseCaseTests
{
    private readonly FakeAccessContext _access = new();
    private readonly FakeBookingRepository _bookings = new();
    private readonly FakeRoomTypeRepository _rooms = new();
    private readonly FakeUpgradeRepository _upgrades = new();
    private readonly FakeRoomInventoryRepository _inventory = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly ModifyBookingUseCase _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _propertyId = Guid.NewGuid();
    private readonly Guid _roomTypeId = Guid.NewGuid();
    private readonly DateOnly _checkIn = new(2026, 8, 20);
    private readonly DateOnly _checkOut = new(2026, 8, 22);

    public ModifyBookingUseCaseTests()
    {
        _sut = new ModifyBookingUseCase(
            _access,
            _bookings,
            _rooms,
            _upgrades,
            new RoomAvailabilityService(_inventory),
            new PricingService(),
            _uow);

        _rooms.Rooms.Add(new AvailableRoomData
        {
            RoomTypeId = _roomTypeId,
            PropertyId = _propertyId,
            Name = "Deluxe",
            MaxAdults = 2,
            MaxChildren = 1,
            MaxGuests = 3,
            PricePerNight = 100m,
            Currency = "USD"
        });
    }

    [Fact]
    public async Task SameStayWhenInventoryFullyBooked_Succeeds()
    {
        _access.Authenticate(_userId);
        var booking = CreateBooking();
        _bookings.Seed(booking);
        _inventory.Seed(_roomTypeId, _checkIn, _checkOut, 0);

        var result = await _sut.ExecuteAsync(new ModifyBookingRequest
        {
            BookingId = booking.Id,
            CheckInDate = _checkIn,
            CheckOutDate = _checkOut,
            Adults = 1,
            Rooms = 1,
            RoomTypeId = _roomTypeId
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Booking.Adults);
        Assert.Equal(1, _uow.SaveChangesCallCount);
        Assert.False(_inventory.CheckAvailabilitySync(_roomTypeId, _checkIn, _checkOut, 1).IsAvailable);
    }

    [Fact]
    public async Task DateChangeWhenCurrentStayFullyBooked_UsesReleasedInventory()
    {
        _access.Authenticate(_userId);
        var booking = CreateBooking();
        _bookings.Seed(booking);
        _inventory.Seed(_roomTypeId, _checkIn, _checkOut, 0);

        var newCheckIn = new DateOnly(2026, 8, 21);
        var newCheckOut = new DateOnly(2026, 8, 23);
        _inventory.Seed(_roomTypeId, _checkOut, newCheckOut, 1);

        var result = await _sut.ExecuteAsync(new ModifyBookingRequest
        {
            BookingId = booking.Id,
            CheckInDate = newCheckIn,
            CheckOutDate = newCheckOut,
            Adults = 2,
            Rooms = 1,
            RoomTypeId = _roomTypeId
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(newCheckIn, result.Value.Booking.CheckInDate);
        Assert.True(_inventory.CheckAvailabilitySync(_roomTypeId, _checkIn, _checkIn.AddDays(1), 1).IsAvailable);
        Assert.False(_inventory.CheckAvailabilitySync(_roomTypeId, newCheckIn, newCheckOut, 1).IsAvailable);
    }

    private BookingData CreateBooking() => new()
    {
        Id = Guid.NewGuid(),
        BookingNumber = "BK123",
        UserId = _userId,
        PropertyId = _propertyId,
        PropertyName = "Hotel",
        CheckInDate = _checkIn,
        CheckOutDate = _checkOut,
        Adults = 2,
        RoomsCount = 1,
        Status = BookingStatus.Confirmed,
        Currency = "USD",
        SubTotal = 200m,
        TotalAmount = 220m,
        CreatedAt = DateTimeOffset.UtcNow,
        Rooms =
        [
            new BookingRoomData
            {
                Id = Guid.NewGuid(),
                RoomTypeId = _roomTypeId,
                RoomTypeName = "Deluxe",
                Quantity = 1,
                PricePerNight = 100m,
                Nights = 2,
                SubTotal = 200m
            }
        ]
    };
}
