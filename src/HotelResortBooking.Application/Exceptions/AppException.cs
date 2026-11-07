namespace HotelResortBooking.Application.Exceptions;

/// <summary>
/// Base exception for application-layer failures.
/// </summary>
public abstract class AppException : Exception
{
    protected AppException(string message)
        : base(message)
    {
    }

    protected AppException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
