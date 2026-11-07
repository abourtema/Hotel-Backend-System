using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace HotelResortBooking.Infrastructure.Services;

public sealed class ImageUrlResolver : IImageUrlResolver
{
    private readonly string _requestPath;
    private readonly string _publicBaseUrl;
    private readonly string[] _legacyPrefixes;

    public ImageUrlResolver(IOptions<StorageOptions> storage, IOptions<FileStorageOptions> fileStorage)
    {
        var storageOptions = storage.Value;
        var files = fileStorage.Value;

        var requestPath = string.IsNullOrWhiteSpace(storageOptions.RequestPath)
            ? "/uploads"
            : storageOptions.RequestPath.Trim();
        _requestPath = NormalizeRequestPath(requestPath);

        _publicBaseUrl = (files.PublicBaseUrl ?? string.Empty).Trim().TrimEnd('/');
        _legacyPrefixes = BuildLegacyPrefixes(files, storageOptions);
    }

    public string ToPublicUrl(string? storedValue)
    {
        if (string.IsNullOrWhiteSpace(storedValue))
        {
            return string.Empty;
        }

        var value = storedValue.Trim().Replace('\\', '/');
        if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        if (value.StartsWith('/')
            && !value.StartsWith("/Users/", StringComparison.OrdinalIgnoreCase)
            && !IsWindowsDrivePath("/" + value.TrimStart('/')))
        {
            return value;
        }

        value = StripFileScheme(value);
        value = StripLegacyPrefixes(value);
        value = value.TrimStart('/');

        if (string.IsNullOrWhiteSpace(value) || value.Contains("..", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        if (value.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
        {
            value = value["uploads/".Length..];
        }

        if (string.IsNullOrWhiteSpace(_publicBaseUrl))
        {
            return $"{_requestPath}/{value}";
        }

        return $"{_publicBaseUrl}/{value}";
    }

    private string StripLegacyPrefixes(string value)
    {
        foreach (var prefix in _legacyPrefixes)
        {
            if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return value[prefix.Length..];
            }
        }

        return value;
    }

    private static string[] BuildLegacyPrefixes(FileStorageOptions files, StorageOptions storage)
    {
        var prefixes = new List<string>();
        AddPrefix(prefixes, files.LegacyBasePath);
        AddPrefix(prefixes, files.LocalPath);
        AddPrefix(prefixes, storage.RootPath);

        return prefixes
            .Select(NormalizePrefix)
            .Where(p => p.Length > 1)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(p => p.Length)
            .ToArray();
    }

    private static void AddPrefix(List<string> prefixes, string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        var value = raw.Trim().Replace('\\', '/');
        prefixes.Add(value);
        prefixes.Add(StripFileScheme(value));

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.IsFile)
        {
            prefixes.Add(uri.LocalPath.Replace('\\', '/'));
        }
    }

    private static string StripFileScheme(string value)
    {
        if (!value.StartsWith("file:", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.IsFile)
        {
            return uri.LocalPath.Replace('\\', '/');
        }

        return value["file:".Length..].TrimStart('/');
    }

    private static string NormalizePrefix(string prefix)
    {
        var value = prefix.Trim().Replace('\\', '/');
        if (!value.EndsWith('/'))
        {
            value += "/";
        }

        return value;
    }

    private static string NormalizeRequestPath(string requestPath)
    {
        var path = requestPath.TrimEnd('/');
        return path.StartsWith('/') ? path : "/" + path;
    }

    private static bool IsWindowsDrivePath(string value)
        => value.Length >= 3 && char.IsLetter(value[0]) && value[1] == ':' && value[2] == '/';
}
