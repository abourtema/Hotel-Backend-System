using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.UseCases.Payments;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.UnitTests.Fakes;
using Xunit;

namespace HotelResortBooking.UnitTests.UseCases;

public sealed class CreatePaymentUseCaseTests
{
    private readonly FakeAccessContext _access = new();
    private readonly FakeBookingRepository _bookings = new();
    private readonly FakePaymentRepository _payments = new();
    private readonly FakePaymentGateway _gateway = new();
    private readonly FakeDateTimeProvider _clock = new() { UtcNow = new DateTimeOffset(2026, 9, 13, 12, 0, 0, TimeSpan.Zero) };
    private readonly FakeUnitOfWork _uow = new();
    private readonly CreatePaymentUseCase _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public CreatePaymentUseCaseTests()
    {
        _sut = new CreatePaymentUseCase(_access, _bookings, _payments, _gateway, _clock, _uow);
    }

    [Fact]
    public async Task Owner_CanCreatePayment()
    {
        _access.Authenticate(_userId);
        var booking = CreateBooking(BookingStatus.Pending, _userId);
        _bookings.Seed(booking);

        var result = await _sut.ExecuteAsync(new CreatePaymentRequest
        {
            BookingId = booking.Id,
            PaymentMethod = PaymentMethodType.PayAtHotel
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(booking.Id, result.Value.BookingId);
        Assert.Equal(_userId, _payments.Payments.Single().UserId);
        Assert.Equal(PaymentStatus.Pending, result.Value.Status);
    }

    [Fact]
    public async Task OtherCustomer_IsForbidden()
    {
        _access.Authenticate(_userId);
        var booking = CreateBooking(BookingStatus.Pending, Guid.NewGuid());
        _bookings.Seed(booking);

        var result = await _sut.ExecuteAsync(new CreatePaymentRequest
        {
            BookingId = booking.Id,
            PaymentMethod = PaymentMethodType.PayAtHotel
        });

        Assert.True(result.IsFailure);
        Assert.Equal("UnauthorizedBookingAccess", result.Error!.Code);
    }

    [Fact]
    public async Task StaffBypass_CreatesPaymentForGuestBooking()
    {
        _access.Authenticate(_userId);
        var guestId = Guid.NewGuid();
        var booking = CreateBooking(BookingStatus.Pending, guestId);
        _bookings.Seed(booking);

        var result = await _sut.ExecuteAsync(new CreatePaymentRequest
        {
            BookingId = booking.Id,
            PaymentMethod = PaymentMethodType.PayAtHotel,
            BypassOwnershipCheck = true
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(guestId, _payments.Payments.Single().UserId);
        Assert.Equal(PaymentStatus.Pending, result.Value.Status);
        Assert.Equal(BookingStatus.Confirmed, _bookings.Bookings.Single().Status);
    }

    [Fact]
    public async Task CreditCard_ChargesGatewayAndSucceeds()
    {
        _access.Authenticate(_userId);
        var booking = CreateBooking(BookingStatus.Pending, _userId);
        _bookings.Seed(booking);

        var result = await _sut.ExecuteAsync(new CreatePaymentRequest
        {
            BookingId = booking.Id,
            PaymentMethod = PaymentMethodType.CreditCard,
            PaymentMethodToken = "tok_visa",
            IdempotencyKey = "pay-card-1"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Succeeded, result.Value.Status);
        Assert.Equal(BookingStatus.Confirmed, _bookings.Bookings.Single().Status);
        Assert.Equal(PaymentStatus.Succeeded, _payments.Payments.Single().Status);
    }

    [Fact]
    public async Task GatewayFailure_LeavesPaymentFailedAndBookingPending()
    {
        _access.Authenticate(_userId);
        var booking = CreateBooking(BookingStatus.Pending, _userId);
        _bookings.Seed(booking);

        var result = await _sut.ExecuteAsync(new CreatePaymentRequest
        {
            BookingId = booking.Id,
            PaymentMethod = PaymentMethodType.MobileWallet,
            IdempotencyKey = "pay-FAIL-wallet"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Failed, result.Value.Status);
        Assert.Equal(BookingStatus.Pending, _bookings.Bookings.Single().Status);
    }

    private BookingData CreateBooking(BookingStatus status, Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        BookingNumber = "BKPAY2",
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
        TaxAmount = 20m,
        TotalAmount = 220m,
        CreatedAt = _clock.UtcNow
    };
}
