namespace HotelResortBooking.Application.Abstractions;

/// <summary>
/// Authenticated identity for the current request. Implemented outside ASP.NET HTTP types.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }

    string? Email { get; }

    IReadOnlyList<string> Roles { get; }

    bool IsAuthenticated { get; }

    bool IsInRole(string role);
}
