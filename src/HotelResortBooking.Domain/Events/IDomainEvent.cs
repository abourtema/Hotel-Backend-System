namespace HotelResortBooking.Domain.Events;

/// <summary>
/// Marker interface for domain events. Concrete events will be added with business modules.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
