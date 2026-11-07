namespace HotelResortBooking.Domain.Common;

/// <summary>
/// Base type for domain entities. Business entities will derive from this later.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();
}
