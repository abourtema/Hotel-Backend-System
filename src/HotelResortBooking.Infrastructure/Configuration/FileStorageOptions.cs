namespace HotelResortBooking.Infrastructure.Configuration;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public string LegacyBasePath { get; set; } = string.Empty;

    public string LocalPath { get; set; } = string.Empty;

    public string PublicBaseUrl { get; set; } = string.Empty;
}
