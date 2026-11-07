using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.UseCases.Payments;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.UnitTests.Fakes;
using Xunit;

namespace HotelResortBooking.UnitTests.UseCases;

public sealed class PaymentUseCaseTests
{
    private readonly FakeAccessContext _access = new();
    private readonly FakeBookingRepository _bookings = new();
    private readonly FakePaymentRepository _payments = new();
    private readonly FakePaymentGateway _gateway = new();
    private readonly FakeDateTimeProvider _clock = new() { UtcNow = new DateTimeOffset(2026, 8, 1, 12, 0, 0, TimeSpan.Zero) };
    private readonly FakeUnitOfWork _uow = new();
    private readonly PayBookingWithCardUseCase _sut;

    private readonly Guid _userId = Guid.NewGuid();

    public PaymentUseCaseTests()
    {
        _sut = new PayBookingWithCardUseCase(
            _access,
            _bookings,
            _payments,
            _gateway,
            _clock,
            _uow);
    }

    [Fact]
    public async Task ValidPayment_Succeeds()
    {
        _access.Authenticate(_userId);
        var booking = CreateBooking(BookingStatus.Pending, _userId);
        _bookings.Seed(booking);

        var result = await _sut.ExecuteAsync(new PayBookingWithCardRequest
        {
            BookingId = booking.Id,
            PaymentMethodToken = "tok_visa",
            IdempotencyKey = "pay-success-1",
            CardBrand = "Visa",
            CardLast4 = "4242"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Succeeded, result.Value.PaymentStatus);
        Assert.Equal(BookingStatus.Confirmed, result.Value.BookingStatus);
        Assert.Equal("pay-success-1", result.Value.ProviderTransactionId);
    }

    [Fact]
    public async Task CancelledBooking_NotPayable()
    {
        _access.Authenticate(_userId);
        var booking = CreateBooking(BookingStatus.Cancelled, _userId);
        _bookings.Seed(booking);

        var result = await _sut.ExecuteAsync(new PayBookingWithCardRequest
        {
            BookingId = booking.Id,
            PaymentMethodToken = "tok_visa",
            IdempotencyKey = "pay-cancelled"
        });

        Assert.True(result.IsFailure);
        Assert.Equal("BookingNotPayable", result.Error!.Code);
    }

    [Fact]
    public async Task DuplicateSucceededPayment_Conflicts()
    {
        _access.Authenticate(_userId);
        var booking = CreateBooking(BookingStatus.Pending, _userId);
        _bookings.Seed(booking);
        _payments.Seed(new PaymentData
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            UserId = _userId,
            Amount = booking.TotalAmount,
            Currency = "USD",
            PaymentMethod = PaymentMethodType.CreditCard,
            Status = PaymentStatus.Succeeded,
            ProviderTransactionId = "prior-success",
            CreatedAt = _clock.UtcNow
        });

        var result = await _sut.ExecuteAsync(new PayBookingWithCardRequest
        {
            BookingId = booking.Id,
            PaymentMethodToken = "tok_visa",
            IdempotencyKey = "pay-dup"
        });

        Assert.True(result.IsFailure);
        Assert.Equal("PaymentAlreadyProcessed", result.Error!.Code);
    }

    [Fact]
    public async Task PaymentFailure_GatewayFail_ReturnsFailedStatus()
    {
        _access.Authenticate(_userId);
        var booking = CreateBooking(BookingStatus.Pending, _userId);
        _bookings.Seed(booking);

        var result = await _sut.ExecuteAsync(new PayBookingWithCardRequest
        {
            BookingId = booking.Id,
            PaymentMethodToken = "tok_visa",
            IdempotencyKey = "pay-FAIL-card"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Failed, result.Value.PaymentStatus);
        Assert.Equal(BookingStatus.Pending, result.Value.BookingStatus);
        Assert.NotNull(result.Value.FailureReason);
    }

    [Fact]
    public async Task UnauthorizedPayment_Fails()
    {
        _access.Authenticate(_userId);
        var booking = CreateBooking(BookingStatus.Pending, Guid.NewGuid());
        _bookings.Seed(booking);

        var result = await _sut.ExecuteAsync(new PayBookingWithCardRequest
        {
            BookingId = booking.Id,
            PaymentMethodToken = "tok_visa",
            IdempotencyKey = "pay-unauth"
        });

        Assert.True(result.IsFailure);
        Assert.Equal("UnauthorizedBookingAccess", result.Error!.Code);
    }

    [Fact]
    public async Task StaffBypass_PaysAnotherUsersBooking()
    {
        _access.Authenticate(_userId);
        var guestId = Guid.NewGuid();
        var booking = CreateBooking(BookingStatus.Pending, guestId);
        _bookings.Seed(booking);

        var result = await _sut.ExecuteAsync(new PayBookingWithCardRequest
        {
            BookingId = booking.Id,
            PaymentMethodToken = "tok_visa",
            IdempotencyKey = "pay-staff",
            BypassOwnershipCheck = true
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(guestId, _payments.Payments.Single().UserId);
    }

    private BookingData CreateBooking(BookingStatus status, Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        BookingNumber = "BKPAY1",
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
        CancellationDeadline = _clock.UtcNow.AddDays(2),
        CreatedAt = _clock.UtcNow
    };
}
