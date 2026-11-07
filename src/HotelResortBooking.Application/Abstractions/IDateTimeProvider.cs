namespace HotelResortBooking.Application.Abstractions;

/// <summary>
/// Abstraction for the current date/time (testable clock).
/// </summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}
