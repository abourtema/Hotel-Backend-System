using HotelResortBooking.Application.Abstractions.Repositories;

namespace HotelResortBooking.UnitTests.Fakes;

public sealed class FakeExternalLoginRepository : IExternalLoginRepository
{
    public List<ExternalLoginData> Logins { get; } = [];

    public Task<ExternalLoginData?> GetByProviderAsync(
        string provider,
        string providerUserId,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Logins.FirstOrDefault(x =>
            x.Provider == provider && x.ProviderUserId == providerUserId));

    public Task AddAsync(
        Guid userId,
        string provider,
        string providerUserId,
        CancellationToken cancellationToken = default)
    {
        Logins.Add(new ExternalLoginData
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Provider = provider,
            ProviderUserId = providerUserId
        });
        return Task.CompletedTask;
    }
}
