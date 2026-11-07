namespace HotelResortBooking.Domain.Entities;

using HotelResortBooking.Domain.Common;

public sealed class UserPreferences : AuditableEntity
{
    public Guid UserId { get; set; }

    public string Language { get; set; } = "en";

    public string Currency { get; set; } = "LYD";

    public bool NotificationsEnabled { get; set; } = true;

    public bool MarketingNotificationsEnabled { get; set; }

    public bool BiometricLoginEnabled { get; set; }

    public User User { get; set; } = null!;
}
