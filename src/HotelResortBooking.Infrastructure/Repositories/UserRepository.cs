using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Domain.Entities;
using HotelResortBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelResortBooking.Infrastructure.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _dbContext;

    public UserRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeEmail(email);
        return _dbContext.Users.AsNoTracking()
            .AnyAsync(u => u.Email == normalized, cancellationToken);
    }

    public async Task<UserAccountData?> GetByEmailAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeEmail(email);
        var user = await _dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalized, cancellationToken);

        return user is null ? null : Map(user);
    }

    public async Task<UserAccountData?> GetByIdAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return user is null ? null : Map(user);
    }

    public async Task<UserAccountData> CreateAsync(
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        string? phoneNumber,
        CancellationToken cancellationToken = default)
    {
        var user = new User
        {
            Email = NormalizeEmail(email),
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber,
            IsActive = true,
            Role = "User",
            Profile = new UserProfile(),
            Preferences = new UserPreferences(),
            LoyaltyAccount = new LoyaltyAccount
            {
                CurrentBalance = 0,
                LifetimeEarned = 0,
                LifetimeRedeemed = 0
            }
        };

        _dbContext.Users.Add(user);
        await Task.CompletedTask;
        return Map(user);
    }

    public async Task UpdateLastLoginAsync(
        Guid userId,
        DateTimeOffset lastLoginAt,
        CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            return;
        }

        user.LastLoginAt = lastLoginAt;
    }

    private static string NormalizeEmail(string email) =>
        email.Trim().ToLowerInvariant();

    private static UserAccountData Map(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        PasswordHash = user.PasswordHash,
        FirstName = user.FirstName,
        LastName = user.LastName,
        PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            Role = string.IsNullOrWhiteSpace(user.Role) ? "User" : user.Role
        };
}
