using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Infrastructure.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace HotelResortBooking.Infrastructure.Services;

public sealed class LocalImageStorage : IImageStorage
{
    private readonly StorageOptions _options;
    private readonly string _rootPath;

    public LocalImageStorage(IOptions<StorageOptions> options, IHostEnvironment environment)
    {
        _options = options.Value;
        _rootPath = _options.GetAbsoluteRootPath(environment.ContentRootPath);
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<Result<string>> SaveAsync(
        string category,
        Guid entityId,
        string originalFileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(originalFileName);
        if (string.IsNullOrWhiteSpace(extension)
            || !_options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            return Result.Failure<string>(BusinessErrors.InvalidImageFile());
        }

        await using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        if (buffer.Length == 0 || buffer.Length > _options.MaxFileSizeBytes)
        {
            return Result.Failure<string>(BusinessErrors.InvalidImageFile());
        }

        var safeCategory = SanitizeSegment(category);
        var relativePath = Path.Combine(safeCategory, entityId.ToString("N"), $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}")
            .Replace('\\', '/');
        var fullPath = Path.Combine(_rootPath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        buffer.Position = 0;
        await using var file = File.Create(fullPath);
        await buffer.CopyToAsync(file, cancellationToken);

        return Result.Success(relativePath);
    }

    public Task DeleteAsync(string? storedPath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(storedPath)
            || storedPath.Contains("://", StringComparison.Ordinal)
            || storedPath.Contains("..", StringComparison.Ordinal))
        {
            return Task.CompletedTask;
        }

        var fullPath = Path.GetFullPath(Path.Combine(_rootPath, storedPath.Replace('/', Path.DirectorySeparatorChar)));
        var rootWithSep = _rootPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(fullPath, _rootPath, StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private static string SanitizeSegment(string value)
    {
        var trimmed = string.IsNullOrWhiteSpace(value) ? "misc" : value.Trim().ToLowerInvariant();
        var chars = trimmed.Select(ch => char.IsLetterOrDigit(ch) ? ch : '-').ToArray();
        return new string(chars);
    }
}
