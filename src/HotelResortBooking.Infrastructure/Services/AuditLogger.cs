using System.Text.Json;
using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Domain.Entities;
using HotelResortBooking.Infrastructure.Persistence;

namespace HotelResortBooking.Infrastructure.Services;

public sealed class AuditLogger : IAuditLogger
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly ApplicationDbContext _db;
    private readonly IAccessContext _access;
    private readonly IAuditContext _auditContext;
    private readonly IDateTimeProvider _clock;

    public AuditLogger(
        ApplicationDbContext db,
        IAccessContext access,
        IAuditContext auditContext,
        IDateTimeProvider clock)
    {
        _db = db;
        _access = access;
        _auditContext = auditContext;
        _clock = clock;
    }

    public Task LogAsync(AuditLogEntry entry, CancellationToken cancellationToken = default)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            UserId = _access.UserId,
            Action = entry.Action,
            EntityType = entry.EntityType,
            EntityId = entry.EntityId,
            OldValues = Serialize(entry.OldValues),
            NewValues = Serialize(entry.NewValues),
            IpAddress = _auditContext.IpAddress,
            UserAgent = _auditContext.UserAgent,
            CreatedAt = _clock.UtcNow
        });

        return Task.CompletedTask;
    }

    private static string? Serialize(object? value)
    {
        if (value is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(value, JsonOptions);
    }
}
