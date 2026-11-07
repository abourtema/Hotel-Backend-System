namespace HotelResortBooking.Domain.Entities;

using HotelResortBooking.Domain.Common;

public sealed class UserProfile : AuditableEntity
{
    public Guid UserId { get; set; }

    public string? ProfilePhotoUrl { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public string? Country { get; set; }

    public string? City { get; set; }

    public string PreferredLanguage { get; set; } = "en";

    public string PreferredCurrency { get; set; } = "LYD";

    public User User { get; set; } = null!;
}
