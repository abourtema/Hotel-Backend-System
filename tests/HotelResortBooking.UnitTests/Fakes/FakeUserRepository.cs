using HotelResortBooking.Application.Abstractions.Repositories;

namespace HotelResortBooking.UnitTests.Fakes;

public sealed class FakeUserRepository : IUserRepository
{
    public List<UserAccountData> Users { get; } = [];

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
        => Task.FromResult(Users.Any(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

    public Task<UserAccountData?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
        => Task.FromResult(Users.FirstOrDefault(u =>
            string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase)));

    public Task<UserAccountData?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult(Users.FirstOrDefault(u => u.Id == userId));

    public Task<UserAccountData> CreateAsync(
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        string? phoneNumber,
        CancellationToken cancellationToken = default)
    {
        var user = new UserAccountData
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = passwordHash,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber,
            IsActive = true,
            Role = "User"
        };
        Users.Add(user);
        return Task.FromResult(user);
    }

    public void Replace(UserAccountData user)
    {
        var index = Users.FindIndex(u => u.Id == user.Id);
        if (index >= 0)
        {
            Users[index] = user;
        }
    }

    public Task UpdateLastLoginAsync(Guid userId, DateTimeOffset lastLoginAt, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
