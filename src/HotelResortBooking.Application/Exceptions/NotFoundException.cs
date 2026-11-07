namespace HotelResortBooking.Application.Exceptions;

/// <summary>
/// Thrown when a requested resource cannot be found.
/// </summary>
public sealed class NotFoundException : AppException
{
    public NotFoundException(string name, object key)
        : base($"Entity \"{name}\" ({key}) was not found.")
    {
        Name = name;
        Key = key;
    }

    public string Name { get; }

    public object Key { get; }
}
