using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.UseCases.Bookings;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.UnitTests.Fakes;
using Xunit;

namespace HotelResortBooking.UnitTests.UseCases;

public sealed class GetUserBookingsUseCaseTests
{
    private readonly FakeAccessContext _access = new();
    private readonly FakeBookingRepository _bookings = new();
    private readonly FakePaymentRepository _payments = new();
    private readonly FakeDateTimeProvider _clock = new()
    {
        UtcNow = new DateTimeOffset(2026, 9, 16, 12, 0, 0, TimeSpan.Zero)
    };
    private readonly GetUserBookingsUseCase _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public GetUserBookingsUseCaseTests()
    {
        _sut = new GetUserBookingsUseCase(_access, _bookings, _payments, _clock);
    }

    [Fact]
    public async Task IncludesSucceededPaymentOnBooking()
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
            PaymentMethod = PaymentMethodType.CreditCard,
            Status = PaymentStatus.Succeeded,
            CreatedAt = _clock.UtcNow
        });

        var result = await _sut.ExecuteAsync(new GetUserBookingsRequest());

        Assert.True(result.IsSuccess);
        var listed = Listed(result.Value);
        Assert.Equal(paymentId, listed.PaymentId);
        Assert.Equal(PaymentStatus.Succeeded, listed.PaymentStatus);
        Assert.Equal(PaymentMethodType.CreditCard, listed.PaymentMethod);
    }

    [Fact]
    public async Task PrefersSucceededPaymentOverLaterFailedAttempt()
    {
        _access.Authenticate(_userId);
        var booking = CreateBooking(_userId);
        _bookings.Seed(booking);
        var succeededId = Guid.NewGuid();
        _payments.Seed(new PaymentData
        {
            Id = succeededId,
            BookingId = booking.Id,
            UserId = _userId,
            Amount = booking.TotalAmount,
            Currency = "USD",
            PaymentMethod = PaymentMethodType.CreditCard,
            Status = PaymentStatus.Succeeded,
            CreatedAt = _clock.UtcNow.AddMinutes(-10)
        });
        _payments.Seed(new PaymentData
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            UserId = _userId,
            Amount = booking.TotalAmount,
            Currency = "USD",
            PaymentMethod = PaymentMethodType.CreditCard,
            Status = PaymentStatus.Failed,
            CreatedAt = _clock.UtcNow
        });

        var result = await _sut.ExecuteAsync(new GetUserBookingsRequest());

        Assert.True(result.IsSuccess);
        var listed = Listed(result.Value);
        Assert.Equal(succeededId, listed.PaymentId);
        Assert.Equal(PaymentStatus.Succeeded, listed.PaymentStatus);
    }

    private static BookingDto Listed(GetUserBookingsResponse response)
        => response.Upcoming
            .Concat(response.Ongoing)
            .Concat(response.Past)
            .DistinctBy(b => b.Id)
            .Single();

    private BookingData CreateBooking(Guid userId) => new()
    {
        Id = Guid.NewGuid(),
        BookingNumber = "BKLIST1",
        UserId = userId,
        PropertyId = Guid.NewGuid(),
        PropertyName = "Hotel",
        CheckInDate = new DateOnly(2026, 9, 16),
        CheckOutDate = new DateOnly(2026, 9, 17),
        Adults = 2,
        RoomsCount = 1,
        Status = BookingStatus.Confirmed,
        Currency = "USD",
        SubTotal = 55m,
        TaxAmount = 5.5m,
        TotalAmount = 60.5m,
        CreatedAt = _clock.UtcNow
    };
}
