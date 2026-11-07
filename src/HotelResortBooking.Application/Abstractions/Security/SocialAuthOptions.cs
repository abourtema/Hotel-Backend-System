namespace HotelResortBooking.Application.Abstractions.Security;

public sealed class GoogleAuthOptions
{
    public const string SectionName = "Authentication:Google";

    public string ClientId { get; set; } = string.Empty;

    public string[] ClientIds { get; set; } = [];

    public IReadOnlyList<string> GetAudiences()
    {
        var audiences = new List<string>();
        if (!string.IsNullOrWhiteSpace(ClientId))
        {
            audiences.Add(ClientId.Trim());
        }

        if (ClientIds is { Length: > 0 })
        {
            audiences.AddRange(ClientIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim()));
        }

        return audiences.Distinct(StringComparer.Ordinal).ToList();
    }
}

public sealed class AppleAuthOptions
{
    public const string SectionName = "Authentication:Apple";

    public string ClientId { get; set; } = string.Empty;

    public string[] ClientIds { get; set; } = [];

    public string TeamId { get; set; } = string.Empty;

    public string KeyId { get; set; } = string.Empty;

    public string PrivateKey { get; set; } = string.Empty;

    public IReadOnlyList<string> GetAudiences()
    {
        var audiences = new List<string>();
        if (!string.IsNullOrWhiteSpace(ClientId))
        {
            audiences.Add(ClientId.Trim());
        }

        if (ClientIds is { Length: > 0 })
        {
            audiences.AddRange(ClientIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim()));
        }

        return audiences.Distinct(StringComparer.Ordinal).ToList();
    }
}
