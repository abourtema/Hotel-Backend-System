using HotelResortBooking.Application.Abstractions;

namespace HotelResortBooking.Infrastructure.Services;

/// <summary>
/// Scoped request identity. Populated by API middleware from JWT claims or guest headers.
/// </summary>
public sealed class AccessContext : IAccessContext, ICurrentUser, IAuditContext
{
    public AccessMode Mode { get; set; } = AccessMode.Initial;

    public Guid? UserId { get; set; }

    public string? Email { get; set; }

    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();

    public string? IpAddress { get; set; }

    public string? UserAgent { get; set; }

    public bool IsAuthenticated => Mode == AccessMode.Authenticated && UserId.HasValue;

    public bool IsInRole(string role) =>
        Roles.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase));
}
