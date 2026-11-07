namespace HotelResortBooking.Domain.Entities;

using HotelResortBooking.Domain.Common;

public sealed class ExternalLogin : Entity
{
    public Guid UserId { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string ProviderUserId { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public User User { get; set; } = null!;
}
