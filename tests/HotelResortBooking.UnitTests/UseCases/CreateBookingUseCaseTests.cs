using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Services;
using HotelResortBooking.Application.UseCases.Bookings;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.UnitTests.Fakes;
using Xunit;

namespace HotelResortBooking.UnitTests.UseCases;

public sealed class CreateBookingUseCaseTests
{
    private readonly FakeAccessContext _access = new();
    private readonly FakePropertyRepository _properties = new();
    private readonly FakeRoomTypeRepository _rooms = new();
    private readonly FakeUpgradeRepository _upgrades = new();
    private readonly FakeRoomInventoryRepository _inventory = new();
    private readonly FakeBookingRepository _bookings = new();
    private readonly FakeLoyaltyRepository _loyalty = new();
    private readonly FakeUserRepository _users = new();
    private readonly FakePaymentRepository _payments = new();
    private readonly FakePaymentGateway _gateway = new();
    private readonly FakeDateTimeProvider _clock = new() { UtcNow = new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero) };
    private readonly FakeUnitOfWork _uow = new();
    private readonly CreateBookingUseCase _sut;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _propertyId = Guid.NewGuid();
    private readonly Guid _roomTypeId = Guid.NewGuid();
    private readonly DateOnly _checkIn = new(2026, 8, 20);
    private readonly DateOnly _checkOut = new(2026, 8, 22);

    public CreateBookingUseCaseTests()
    {
        _sut = new CreateBookingUseCase(
            _access,
            _properties,
            _rooms,
            _upgrades,
            new RoomAvailabilityService(_inventory),
            new PricingService(),
            _bookings,
            _loyalty,
            _users,
            _payments,
            _gateway,
            _clock,
            _uow);

        SeedHappyPath();
    }

    [Fact]
    public async Task ValidBooking_WhenAuthenticated_Succeeds()
    {
        _access.Authenticate(_userId);

        var result = await _sut.ExecuteAsync(ValidRequest());

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Pending, result.Value.Booking.Status);
        Assert.Equal(_userId, result.Value.Booking.UserId);
        Assert.Equal("Hatim", result.Value.Booking.Guest!.FirstName);
        Assert.Equal("abu irtaymah", result.Value.Booking.Guest.LastName);
        Assert.Equal("airport transfer", result.Value.Booking.SpecialRequests);
        Assert.Single(result.Value.Booking.AdditionalGuests);
        Assert.Equal(1, _uow.SaveChangesCallCount);
        Assert.Single(_bookings.Bookings);
    }

    [Fact]
    public async Task MissingPrimaryGuest_UsesProfileAndSucceeds()
    {
        _access.Authenticate(_userId);

        var result = await _sut.ExecuteAsync(new CreateBookingRequest
        {
            PropertyId = _propertyId,
            RoomTypeId = _roomTypeId,
            CheckInDate = _checkIn,
            CheckOutDate = _checkOut,
            Adults = 2,
            Rooms = 1
        });

        Assert.True(result.IsSuccess);
        Assert.Equal("Hatim", result.Value.Booking.Guest!.FirstName);
        Assert.Equal("abourtemah@gmail.com", result.Value.Booking.Guest.Email);
    }

    [Fact]
    public async Task PayAtHotel_LeavesBookingPending()
    {
        _access.Authenticate(_userId);

        var request = ValidRequest();
        request = new CreateBookingRequest
        {
            PropertyId = request.PropertyId,
            RoomTypeId = request.RoomTypeId,
            CheckInDate = request.CheckInDate,
            CheckOutDate = request.CheckOutDate,
            Adults = request.Adults,
            Children = request.Children,
            Rooms = request.Rooms,
            Guest = request.Guest,
            AdditionalGuests = request.AdditionalGuests,
            SpecialRequests = request.SpecialRequests,
            Payment = new BookingCheckoutPaymentInput
            {
                Method = PaymentMethodType.PayAtHotel
            }
        };

        var result = await _sut.ExecuteAsync(request);

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Pending, result.Value.Booking.Status);
        Assert.Null(result.Value.PaymentId);
        Assert.Empty(_payments.Payments);
    }

    [Fact]
    public async Task CreditCardWithoutToken_CompletesBookingAndAddsPayment()
    {
        _access.Authenticate(_userId);

        var request = ValidRequest();
        var result = await _sut.ExecuteAsync(new CreateBookingRequest
        {
            PropertyId = request.PropertyId,
            RoomTypeId = request.RoomTypeId,
            CheckInDate = request.CheckInDate,
            CheckOutDate = request.CheckOutDate,
            Adults = request.Adults,
            Children = request.Children,
            Rooms = request.Rooms,
            Guest = request.Guest,
            AdditionalGuests = request.AdditionalGuests,
            SpecialRequests = request.SpecialRequests,
            Payment = new BookingCheckoutPaymentInput
            {
                Method = PaymentMethodType.CreditCard
            }
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(BookingStatus.Completed, result.Value.Booking.Status);
        Assert.NotNull(result.Value.PaymentId);
        Assert.Equal(PaymentMethodType.CreditCard, result.Value.PaymentMethod);
        Assert.Equal(PaymentStatus.Pending, result.Value.PaymentStatus);
        Assert.Single(_payments.Payments);
    }

    [Fact]
    public async Task Unauthenticated_FailsAuthenticationRequired()
    {
        _access.AsGuest();

        var result = await _sut.ExecuteAsync(ValidRequest());

        Assert.True(result.IsFailure);
        Assert.Equal("AuthenticationRequired", result.Error!.Code);
    }

    [Fact]
    public async Task UnavailableRoom_Fails()
    {
        _access.Authenticate(_userId);
        _inventory.Seed(_roomTypeId, _checkIn, _checkOut, 0);

        var result = await _sut.ExecuteAsync(ValidRequest());

        Assert.True(result.IsFailure);
        Assert.Equal("RoomUnavailable", result.Error!.Code);
    }

    [Fact]
    public async Task InvalidDates_Fails()
    {
        _access.Authenticate(_userId);

        var result = await _sut.ExecuteAsync(new CreateBookingRequest
        {
            PropertyId = _propertyId,
            RoomTypeId = _roomTypeId,
            CheckInDate = _checkOut,
            CheckOutDate = _checkIn,
            Adults = 2,
            Rooms = 1
        });

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidDateRange", result.Error!.Code);
    }

    private CreateBookingRequest ValidRequest() => new()
    {
        PropertyId = _propertyId,
        RoomTypeId = _roomTypeId,
        CheckInDate = _checkIn,
        CheckOutDate = _checkOut,
        Adults = 2,
        Children = 0,
        Rooms = 1,
        Guest = new BookingGuestInput
        {
            FirstName = "Hatim",
            LastName = "abu irtaymah",
            Email = "abourtemah@gmail.com",
            PhoneNumber = "+218"
        },
        AdditionalGuests =
        [
            new BookingGuestInput
            {
                FirstName = "Guest",
                LastName = "Two"
            }
        ],
        SpecialRequests = "airport transfer"
    };

    private void SeedHappyPath()
    {
        _properties.Seed(new PropertyDetailsData
        {
            Id = _propertyId,
            Name = "Test Hotel",
            Description = "Desc",
            PropertyType = PropertyType.Hotel,
            City = "Paris",
            Country = "France",
            AddressLine = "1 Rue",
            CheckInTime = new TimeOnly(15, 0),
            CheckOutTime = new TimeOnly(11, 0)
        });

        _rooms.Rooms.Add(new AvailableRoomData
        {
            RoomTypeId = _roomTypeId,
            PropertyId = _propertyId,
            Name = "Deluxe",
            Description = "Room",
            MaxAdults = 2,
            MaxChildren = 1,
            MaxGuests = 3,
            PricePerNight = 150m,
            Currency = "USD",
            AvailableCount = 5
        });

        _inventory.Seed(_roomTypeId, _checkIn, _checkOut, 5);
        _bookings.PropertyNameOverride = "Test Hotel";
        _users.Users.Add(new UserAccountData
        {
            Id = _userId,
            Email = "abourtemah@gmail.com",
            FirstName = "Hatim",
            LastName = "abu irtaymah",
            PhoneNumber = "+218",
            IsActive = true,
            Role = "User"
        });
    }
}
