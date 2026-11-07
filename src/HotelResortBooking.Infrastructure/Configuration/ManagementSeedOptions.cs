namespace HotelResortBooking.Infrastructure.Configuration;

public sealed class ManagementSeedOptions
{
    public const string SectionName = "ManagementSeed";

    public bool Enabled { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;
}
