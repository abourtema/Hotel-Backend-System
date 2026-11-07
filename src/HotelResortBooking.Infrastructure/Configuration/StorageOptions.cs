namespace HotelResortBooking.Infrastructure.Configuration;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    public string RootPath { get; set; } = "uploads";

    public string RequestPath { get; set; } = "/uploads";

    public long MaxFileSizeBytes { get; set; } = 5_242_880;

    public string[] AllowedExtensions { get; set; } = [".jpg", ".jpeg", ".png", ".webp", ".gif"];

    public string GetAbsoluteRootPath(string contentRootPath)
    {
        return Path.IsPathRooted(RootPath)
            ? Path.GetFullPath(RootPath)
            : Path.GetFullPath(Path.Combine(contentRootPath, RootPath));
    }
}
