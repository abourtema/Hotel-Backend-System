namespace HotelResortBooking.Application.Abstractions;

/// <summary>
/// Abstraction for the unit of work pattern. Implemented in Infrastructure.
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
