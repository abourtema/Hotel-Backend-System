using HotelResortBooking.Infrastructure.Configuration;
using HotelResortBooking.Infrastructure.Services;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace HotelResortBooking.UnitTests.UseCases;

public sealed class LocalImageStorageTests
{
    [Fact]
    public async Task Save_RejectsDisallowedExtension()
    {
        using var harness = new StorageHarness();
        await using var stream = new MemoryStream("x"u8.ToArray());

        var result = await harness.Storage.SaveAsync("hotels", Guid.NewGuid(), "notes.txt", stream);

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidImage", result.Error!.Code);
    }

    [Fact]
    public async Task Save_WritesFileAndDelete_RemovesIt()
    {
        using var harness = new StorageHarness();
        var entityId = Guid.NewGuid();
        await using var stream = new MemoryStream("image-bytes"u8.ToArray());

        var saved = await harness.Storage.SaveAsync("hotels", entityId, "pool.PNG", stream);

        Assert.True(saved.IsSuccess);
        Assert.EndsWith(".png", saved.Value, StringComparison.OrdinalIgnoreCase);
        var fullPath = Path.Combine(harness.Root, saved.Value.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(fullPath));

        await harness.Storage.DeleteAsync(saved.Value);
        Assert.False(File.Exists(fullPath));
    }

    [Fact]
    public void Resolver_PrefixesLocalPaths_AndLeavesAbsoluteUrls()
    {
        var resolver = new ImageUrlResolver(
            Options.Create(new StorageOptions { RequestPath = "/uploads" }),
            Options.Create(new FileStorageOptions()));

        Assert.Equal("https://cdn.example/a.jpg", resolver.ToPublicUrl("https://cdn.example/a.jpg"));
        Assert.Equal("/uploads/hotels/a.jpg", resolver.ToPublicUrl("hotels/a.jpg"));
    }

    private sealed class StorageHarness : IDisposable
    {
        public string Root { get; } = Path.Combine(Path.GetTempPath(), "hrb-images-" + Guid.NewGuid().ToString("N"));

        public LocalImageStorage Storage { get; }

        public StorageHarness()
        {
            Directory.CreateDirectory(Root);
            var options = Options.Create(new StorageOptions
            {
                RootPath = Root,
                RequestPath = "/uploads",
                MaxFileSizeBytes = 1024,
                AllowedExtensions = [".jpg", ".jpeg", ".png", ".webp", ".gif"]
            });
            Storage = new LocalImageStorage(options, new TestHostEnvironment { ContentRootPath = Root });
        }

        public void Dispose()
        {
            if (Directory.Exists(Root))
            {
                Directory.Delete(Root, recursive: true);
            }
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";

        public string ApplicationName { get; set; } = "Tests";

        public string ContentRootPath { get; set; } = string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
