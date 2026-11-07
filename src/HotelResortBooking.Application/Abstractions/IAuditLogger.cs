namespace HotelResortBooking.Application.Abstractions;

public interface IAuditContext
{
    string? IpAddress { get; }

    string? UserAgent { get; }
}

public sealed class AuditLogEntry
{
    public string Action { get; init; } = string.Empty;

    public string EntityType { get; init; } = string.Empty;

    public string? EntityId { get; init; }

    public object? OldValues { get; init; }

    public object? NewValues { get; init; }
}

public interface IAuditLogger
{
    Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default);
}
