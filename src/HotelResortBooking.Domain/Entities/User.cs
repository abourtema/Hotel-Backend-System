namespace HotelResortBooking.Domain.Entities;

using HotelResortBooking.Domain.Common;

public sealed class User : AggregateRoot
{
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? PhoneNumber { get; set; }

    public bool IsEmailVerified { get; set; }

    public bool IsPhoneVerified { get; set; }

    public string Role { get; set; } = "User";

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

    public UserProfile? Profile { get; set; }

    public UserPreferences? Preferences { get; set; }

    public LoyaltyAccount? LoyaltyAccount { get; set; }

    public ICollection<PaymentMethod> PaymentMethods { get; set; } = new List<PaymentMethod>();

    public ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public ICollection<ExternalLogin> ExternalLogins { get; set; } = new List<ExternalLogin>();

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
