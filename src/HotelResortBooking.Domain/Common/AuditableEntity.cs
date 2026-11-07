namespace HotelResortBooking.Domain.Common;

/// <summary>
/// Entity with creation and last-update timestamps.
/// </summary>
public abstract class AuditableEntity : Entity
{
    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
