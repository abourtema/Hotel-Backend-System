using HotelResortBooking.Application.Abstractions;

namespace HotelResortBooking.UnitTests.Fakes;

public sealed class FakeDateTimeProvider : IDateTimeProvider
{
    public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.UtcNow;
}
