using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.UseCases.Payments;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.UnitTests.Fakes;
using Xunit;

namespace HotelResortBooking.UnitTests.UseCases;

public sealed class ConfirmPayAtHotelPaymentUseCaseTests
{
    private readonly FakeBookingRepository _bookings = new();
    private readonly FakePaymentRepository _payments = new();
    private readonly FakeDateTimeProvider _clock = new() { UtcNow = new DateTimeOffset(2026, 9, 13, 18, 0, 0, TimeSpan.Zero) };
    private readonly FakeUnitOfWork _uow = new();
    private readonly ConfirmPayAtHotelPaymentUseCase _sut;

    public ConfirmPayAtHotelPaymentUseCaseTests()
    {
        _sut = new ConfirmPayAtHotelPaymentUseCase(_payments, _bookings, _clock, _uow);
    }

    [Fact]
    public async Task PayAtHotelPending_SucceedsAndConfirmsBooking()
    {
        var booking = CreateBooking(BookingStatus.Pending);
        _bookings.Seed(booking);
        var payment = await _payments.AddAsync(new CreatePaymentModel
        {
            BookingId = booking.Id,
            UserId = booking.UserId,
            Amount = booking.TotalAmount,
            Currency = booking.Currency,
            PaymentMethod = PaymentMethodType.PayAtHotel,
            Provider = "hotel"
        });

        var result = await _sut.ExecuteAsync(new ConfirmPayAtHotelPaymentRequest { PaymentId = payment.Id });

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Succeeded, result.Value.Status);
        Assert.Equal(BookingStatus.Confirmed, result.Value.BookingStatus);
        Assert.Equal(_clock.UtcNow, result.Value.PaidAt);
        Assert.Equal(PaymentStatus.Succeeded, _payments.Payments.Single().Status);
    }

    [Fact]
    public async Task CreditCardPayment_IsNotConfirmable()
    {
        var booking = CreateBooking(BookingStatus.Pending);
        _bookings.Seed(booking);
        var payment = await _payments.AddAsync(new CreatePaymentModel
        {
            BookingId = booking.Id,
            Amount = booking.TotalAmount,
            Currency = booking.Currency,
            PaymentMethod = PaymentMethodType.CreditCard
        });

        var result = await _sut.ExecuteAsync(new ConfirmPayAtHotelPaymentRequest { PaymentId = payment.Id });

        Assert.True(result.IsFailure);
        Assert.Equal("PaymentNotConfirmable", result.Error!.Code);
    }

    [Fact]
    public async Task AlreadySucceeded_IsIdempotent()
    {
        var booking = CreateBooking(BookingStatus.Confirmed);
        _bookings.Seed(booking);
        var paymentId = Guid.NewGuid();
        _payments.Seed(new PaymentData
        {
            Id = paymentId,
            BookingId = booking.Id,
            Amount = booking.TotalAmount,
            Currency = booking.Currency,
            PaymentMethod = PaymentMethodType.PayAtHotel,
            Status = PaymentStatus.Succeeded,
            PaidAt = _clock.UtcNow,
            CreatedAt = _clock.UtcNow
        });

        var result = await _sut.ExecuteAsync(new ConfirmPayAtHotelPaymentRequest { PaymentId = paymentId });

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Succeeded, result.Value.Status);
        Assert.Single(_payments.Payments);
    }

    private BookingData CreateBooking(BookingStatus status) => new()
    {
        Id = Guid.NewGuid(),
        BookingNumber = "BKHOTEL1",
        UserId = Guid.NewGuid(),
        PropertyId = Guid.NewGuid(),
        PropertyName = "Hotel",
        CheckInDate = new DateOnly(2026, 9, 20),
        CheckOutDate = new DateOnly(2026, 9, 22),
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
