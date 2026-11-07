namespace HotelResortBooking.Application.Abstractions;

public enum AccessMode
{
    Initial = 0,
    Guest = 1,
    Authenticated = 2
}

public interface IAccessContext
{
    AccessMode Mode { get; }

    Guid? UserId { get; }

    bool IsAuthenticated { get; }
}
