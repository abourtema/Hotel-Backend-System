using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Domain.Entities;
using HotelResortBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelResortBooking.Infrastructure.Repositories;

public sealed class ExternalLoginRepository : IExternalLoginRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ExternalLoginRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ExternalLoginData?> GetByProviderAsync(
        string provider,
        string providerUserId,
        CancellationToken cancellationToken = default)
    {
        var login = await _dbContext.ExternalLogins.AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.Provider == provider && x.ProviderUserId == providerUserId,
                cancellationToken);

        return login is null
            ? null
            : new ExternalLoginData
            {
                Id = login.Id,
                UserId = login.UserId,
                Provider = login.Provider,
                ProviderUserId = login.ProviderUserId
            };
    }

    public async Task AddAsync(
        Guid userId,
        string provider,
        string providerUserId,
        CancellationToken cancellationToken = default)
    {
        _dbContext.ExternalLogins.Add(new ExternalLogin
        {
            UserId = userId,
            Provider = provider,
            ProviderUserId = providerUserId
        });

        await Task.CompletedTask;
    }
}
