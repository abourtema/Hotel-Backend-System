using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.Abstractions;

public static class ImageStorageCategories
{
    public const string Hotels = "hotels";

    public const string Rooms = "rooms";

    public const string Reviews = "reviews";
}

public sealed class UploadedImageFile
{
    public string FileName { get; init; } = string.Empty;

    public Stream Content { get; init; } = Stream.Null;
}

public interface IImageStorage
{
    Task<Result<string>> SaveAsync(
        string category,
        Guid entityId,
        string originalFileName,
        Stream content,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(string? storedPath, CancellationToken cancellationToken = default);
}

public interface IImageUrlResolver
{
    string ToPublicUrl(string? storedValue);
}
