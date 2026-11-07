using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.UnitTests.Fakes;

public sealed class FakeImageStorage : IImageStorage
{
    public List<string> SavedPaths { get; } = [];
    public List<string> DeletedPaths { get; } = [];
    public Result<string>? NextResult { get; set; }

    public Task<Result<string>> SaveAsync(
        string category,
        Guid entityId,
        string originalFileName,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        if (NextResult is not null)
        {
            return Task.FromResult(NextResult);
        }

        var path = $"{category}/{entityId:N}/{originalFileName}";
        SavedPaths.Add(path);
        return Task.FromResult(Result.Success(path));
    }

    public Task DeleteAsync(string? storedPath, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(storedPath))
        {
            DeletedPaths.Add(storedPath);
        }

        return Task.CompletedTask;
    }
}

public sealed class FakeImageUrlResolver : IImageUrlResolver
{
    public string ToPublicUrl(string? storedValue)
    {
        if (string.IsNullOrWhiteSpace(storedValue))
        {
            return string.Empty;
        }

        var value = storedValue.Trim();
        if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith('/'))
        {
            return value;
        }

        return $"/uploads/{value.TrimStart('/')}";
    }
}
