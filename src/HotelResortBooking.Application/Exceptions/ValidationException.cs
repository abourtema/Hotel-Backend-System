namespace HotelResortBooking.Application.Exceptions;

/// <summary>
/// Thrown when one or more validation failures occur.
/// </summary>
public sealed class ValidationException : AppException
{
    public ValidationException()
        : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IDictionary<string, string[]> errors)
        : this()
    {
        Errors = errors;
    }

    public IDictionary<string, string[]> Errors { get; }
}
