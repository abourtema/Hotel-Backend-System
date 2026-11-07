namespace HotelResortBooking.Infrastructure.Configuration;

public sealed class AuthenticationOptions
{
    public const string SectionName = "Authentication";

    public string Authority { get; set; } = string.Empty;

    public string Audience { get; set; } = "HotelResortBooking.Api";

    public bool RequireHttpsMetadata { get; set; } = true;
}
