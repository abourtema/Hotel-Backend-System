namespace HotelResortBooking.Application.Abstractions.Repositories;

public sealed class ExternalLoginData
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public string Provider { get; init; } = string.Empty;

    public string ProviderUserId { get; init; } = string.Empty;
}

public interface IExternalLoginRepository
{
    Task<ExternalLoginData?> GetByProviderAsync(
        string provider,
        string providerUserId,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Guid userId,
        string provider,
        string providerUserId,
        CancellationToken cancellationToken = default);
}
