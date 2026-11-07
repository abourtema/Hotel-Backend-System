using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.Abstractions.Repositories;

public sealed class UserAccountData
{
    public Guid Id { get; init; }

    public string Email { get; init; } = string.Empty;

    public string PasswordHash { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string? PhoneNumber { get; init; }

    public bool IsActive { get; init; }

    public string Role { get; init; } = "User";
}

public interface IUserRepository
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<UserAccountData?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<UserAccountData?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<UserAccountData> CreateAsync(
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        string? phoneNumber,
        CancellationToken cancellationToken = default);

    Task UpdateLastLoginAsync(Guid userId, DateTimeOffset lastLoginAt, CancellationToken cancellationToken = default);
}

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string passwordHash);
}

public sealed class WishlistItemData
{
    public Guid Id { get; init; }

    public Guid WishlistId { get; init; }

    public Guid PropertyId { get; init; }

    public string PropertyName { get; init; } = string.Empty;

    public string? MainImageUrl { get; init; }

    public string City { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class WishlistData
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public string Name { get; init; } = string.Empty;

    public IReadOnlyList<WishlistItemData> Items { get; init; } = Array.Empty<WishlistItemData>();
}

public interface IWishlistRepository
{
    Task<WishlistData> GetOrCreateDefaultAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> ContainsPropertyAsync(Guid wishlistId, Guid propertyId, CancellationToken cancellationToken = default);

    Task<WishlistItemData> AddItemAsync(Guid wishlistId, Guid propertyId, CancellationToken cancellationToken = default);

    Task<bool> RemoveItemAsync(Guid wishlistId, Guid propertyId, CancellationToken cancellationToken = default);
}

public sealed class ReviewData
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public Guid PropertyId { get; init; }

    public Guid BookingId { get; init; }

    public int Rating { get; init; }

    public string? Comment { get; init; }

    public ReviewStatus Status { get; init; }

    public IReadOnlyList<string> Tags { get; init; } = Array.Empty<string>();

    public IReadOnlyList<string> PhotoUrls { get; init; } = Array.Empty<string>();
}

public interface IReviewRepository
{
    Task<bool> ExistsForBookingAsync(Guid bookingId, Guid userId, CancellationToken cancellationToken = default);

    Task<ReviewData> AddAsync(
        Guid userId,
        Guid propertyId,
        Guid bookingId,
        int rating,
        string? comment,
        IReadOnlyCollection<string> tags,
        IReadOnlyCollection<string> photoUrls,
        CancellationToken cancellationToken = default);
}
