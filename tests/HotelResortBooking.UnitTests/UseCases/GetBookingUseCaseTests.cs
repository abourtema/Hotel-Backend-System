using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.UseCases.Bookings;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.UnitTests.Fakes;
using Xunit;

namespace HotelResortBooking.UnitTests.UseCases;

public sealed class GetBookingUseCaseTests
{
    private readonly FakeAccessContext _access = new();
    private readonly FakeBookingRepository _bookings = new();
    private readonly FakePaymentRepository _payments = new();
    private readonly GetBookingUseCase _sut;

    private readonly Guid _userId = Guid.NewGuid();

    public GetBookingUseCaseTests()
    {
        _sut = new GetBookingUseCase(_access, _bookings, _payments);
    }

    [Fact]
    public async Task GetBooking_OwnershipCheck_SucceedsForOwner()
    {
        _access.Authenticate(_userId);
        var booking = CreateBooking(_userId);
        _bookings.Seed(booking);

        var result = await _sut.ExecuteAsync(new GetBookingRequest { BookingId = booking.Id });

        Assert.True(result.IsSuccess);
        Assert.Equal(booking.Id, result.Value.Booking.Id);
        Assert.Equal(_userId, result.Value.Booking.UserId);
    }

    [Fact]
    public async Task GetBooking_UnauthorizedAccess_Fails()
    {
        _access.Authenticate(_userId);
        var booking = CreateBooking(Guid.NewGuid());
        _bookings.Seed(booking);

        var result = await _sut.ExecuteAsync(new GetBookingRequest { BookingId = booking.Id });

        Assert.True(result.IsFailure);
        Assert.Equal("UnauthorizedBookingAccess", result.Error!.Code);
    }

    [Fact]
    public async Task GetBooking_ByNumber_Succeeds()
    {
        _access.Authenticate(_userId);
        var booking = CreateBooking(_userId);
        _bookings.Seed(booking);

        var result = await _sut.ExecuteAsync(new GetBookingRequest { BookingNumber = booking.BookingNumber });

        Assert.True(result.IsSuccess);
        Assert.Equal(booking.BookingNumber, result.Value.Booking.BookingNumber);
    }

    [Fact]
    public async Task GetBooking_IncludesPaymentWhenPresent()
    {
        _access.Authenticate(_userId);
        var booking = CreateBooking(_userId);
        _bookings.Seed(booking);
        var paymentId = Guid.NewGuid();
        _payments.Seed(new PaymentData
        {
            Id = paymentId,
            BookingId = booking.Id,
            UserId = _userId,
            Amount = booking.TotalAmount,
            Currency = "USD",
            PaymentMethod = PaymentMethodType.PayAtHotel,
            Status = PaymentStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        });

        var result = await _sut.ExecuteAsync(new GetBookingRequest { BookingId = booking.Id });

        Assert.True(result.IsSuccess);
        Assert.Equal(paymentId, result.Value.Booking.PaymentId);
        Assert.Equal(PaymentStatus.Pending, result.Value.Booking.PaymentStatus);
        Assert.Equal(PaymentMethodType.PayAtHotel, result.Value.Booking.PaymentMethod);
    }

    private BookingData CreateBooking(Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        BookingNumber = "BKOWNER1",
        UserId = userId,
        PropertyId = Guid.NewGuid(),
        PropertyName = "Hotel",
        CheckInDate = new DateOnly(2026, 8, 20),
        CheckOutDate = new DateOnly(2026, 8, 22),
        Adults = 2,
        RoomsCount = 1,
        Status = BookingStatus.Confirmed,
        Currency = "USD",
        SubTotal = 200m,
        TotalAmount = 220m,
        CreatedAt = DateTimeOffset.UtcNow
    };
}
