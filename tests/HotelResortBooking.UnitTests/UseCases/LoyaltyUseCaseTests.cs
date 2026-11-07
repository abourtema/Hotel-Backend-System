using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Services;
using HotelResortBooking.Application.UseCases.Loyalty;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.UnitTests.Fakes;
using Xunit;

namespace HotelResortBooking.UnitTests.UseCases;

public sealed class LoyaltyUseCaseTests
{
    private readonly FakeAccessContext _access = new();
    private readonly FakeLoyaltyRepository _loyalty = new();
    private readonly FakeBookingRepository _bookings = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly PricingService _pricing = new();
    private readonly RedeemPointsUseCase _redeem;
    private readonly ApplyLoyaltyPointsToBookingUseCase _apply;

    private readonly Guid _userId = Guid.NewGuid();

    public LoyaltyUseCaseTests()
    {
        _redeem = new RedeemPointsUseCase(_access, _loyalty, _bookings, _pricing, _uow);
        _apply = new ApplyLoyaltyPointsToBookingUseCase(_access, _loyalty, _bookings, _pricing, _uow);
    }

    [Fact]
    public async Task ValidRedemption_Succeeds()
    {
        _access.Authenticate(_userId);
        _loyalty.SeedBalance(_userId, 1000);
        var booking = CreatePendingBooking(totalPointsRedeemed: 0);
        _bookings.Seed(booking);

        var result = await _redeem.ExecuteAsync(new RedeemPointsRequest
        {
            BookingId = booking.Id,
            Points = 200,
            Description = "Test redeem"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(200, result.Value.PointsRedeemed);
        Assert.Equal(800, result.Value.RemainingBalance);
    }

    [Fact]
    public async Task ApplyLoyaltyPointsToBooking_Succeeds()
    {
        _access.Authenticate(_userId);
        _loyalty.SeedBalance(_userId, 500);
        var booking = CreatePendingBooking(totalPointsRedeemed: 0);
        _bookings.Seed(booking);

        var result = await _apply.ExecuteAsync(new ApplyLoyaltyPointsToBookingRequest
        {
            BookingId = booking.Id,
            Points = 100
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(100, result.Value.PointsApplied);
        Assert.Equal(1m, result.Value.PointsDiscount);
    }

    [Fact]
    public async Task InsufficientPoints_Fails()
    {
        _access.Authenticate(_userId);
        _loyalty.SeedBalance(_userId, 50);
        var booking = CreatePendingBooking(totalPointsRedeemed: 0);
        _bookings.Seed(booking);

        var result = await _redeem.ExecuteAsync(new RedeemPointsRequest
        {
            BookingId = booking.Id,
            Points = 200
        });

        Assert.True(result.IsFailure);
        Assert.Equal("InsufficientLoyaltyPoints", result.Error!.Code);
    }

    [Fact]
    public async Task InvalidNegativeAmount_Fails()
    {
        _access.Authenticate(_userId);
        _loyalty.SeedBalance(_userId, 500);
        var booking = CreatePendingBooking(totalPointsRedeemed: 0);
        _bookings.Seed(booking);

        var result = await _redeem.ExecuteAsync(new RedeemPointsRequest
        {
            BookingId = booking.Id,
            Points = -10
        });

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidPoints", result.Error!.Code);
    }

    private BookingData CreatePendingBooking(int totalPointsRedeemed) => new()
    {
        Id = Guid.NewGuid(),
        BookingNumber = "BKLOY1",
        UserId = _userId,
        PropertyId = Guid.NewGuid(),
        PropertyName = "Hotel",
        CheckInDate = new DateOnly(2026, 8, 20),
        CheckOutDate = new DateOnly(2026, 8, 22),
        Adults = 2,
        RoomsCount = 1,
        Status = BookingStatus.Pending,
        Currency = "USD",
        SubTotal = 200m,
        UpgradeTotal = 0m,
        DiscountAmount = 0m,
        PointsRedeemed = totalPointsRedeemed,
        TaxAmount = 20m,
        TotalAmount = 220m,
        CreatedAt = DateTimeOffset.UtcNow,
        Rooms =
        [
            new BookingRoomData
            {
                Id = Guid.NewGuid(),
                RoomTypeId = Guid.NewGuid(),
                RoomTypeName = "Deluxe",
                Quantity = 1,
                PricePerNight = 100m,
                Nights = 2,
                SubTotal = 200m
            }
        ],
        Upgrades = []
    };
}
