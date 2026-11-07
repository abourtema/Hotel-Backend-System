using HotelResortBooking.Infrastructure.Configuration;
using HotelResortBooking.Infrastructure.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace HotelResortBooking.UnitTests.UseCases;

public sealed class ImageUrlResolverTests
{
    private static ImageUrlResolver CreateResolver(
        string publicBaseUrl = "http://localhost:8056/uploads",
        string legacyBasePath = "file:///Users/hatim/Documents/TestImgesHotal/",
        string localPath = "/app/uploads")
        => new(
            Options.Create(new StorageOptions { RequestPath = "/uploads", RootPath = localPath }),
            Options.Create(new FileStorageOptions
            {
                LegacyBasePath = legacyBasePath,
                LocalPath = localPath,
                PublicBaseUrl = publicBaseUrl
            }));

    [Fact]
    public void Empty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, CreateResolver().ToPublicUrl(null));
        Assert.Equal(string.Empty, CreateResolver().ToPublicUrl("  "));
    }

    [Fact]
    public void HttpAndHttps_Unchanged()
    {
        var resolver = CreateResolver();
        Assert.Equal("https://cdn.example/a.jpg", resolver.ToPublicUrl("https://cdn.example/a.jpg"));
        Assert.Equal("http://localhost:8056/uploads/hotels/a.jpg", resolver.ToPublicUrl("http://localhost:8056/uploads/hotels/a.jpg"));
    }

    [Fact]
    public void LegacyFileUri_MapsToPublicHttpUrl()
    {
        var resolver = CreateResolver();
        var stored = "file:///Users/hatim/Documents/TestImgesHotal/uploads/hotels/61e6e39bd2fa4f5f84ca81e05788b6a0/exterior.jpg";

        Assert.Equal(
            "http://localhost:8056/uploads/hotels/61e6e39bd2fa4f5f84ca81e05788b6a0/exterior.jpg",
            resolver.ToPublicUrl(stored));
    }

    [Fact]
    public void RelativePath_MapsToPublicHttpUrl()
    {
        var resolver = CreateResolver();
        Assert.Equal(
            "http://localhost:8056/uploads/hotels/61e6e39bd2fa4f5f84ca81e05788b6a0/exterior.jpg",
            resolver.ToPublicUrl("hotels/61e6e39bd2fa4f5f84ca81e05788b6a0/exterior.jpg"));
    }

    [Fact]
    public void WindowsSeparators_AreNormalized()
    {
        var resolver = CreateResolver();
        Assert.Equal(
            "http://localhost:8056/uploads/hotels/a/exterior.jpg",
            resolver.ToPublicUrl(@"hotels\a\exterior.jpg"));
    }

    [Fact]
    public void ParentDirectorySegments_AreRejected()
    {
        Assert.Equal(string.Empty, CreateResolver().ToPublicUrl("hotels/../secret.jpg"));
    }

    [Fact]
    public void ProductionPublicBaseUrl_CanBeChangedByConfig()
    {
        var resolver = CreateResolver(publicBaseUrl: "https://api.example.com/uploads");
        Assert.Equal(
            "https://api.example.com/uploads/hotels/a/exterior.jpg",
            resolver.ToPublicUrl("file:///Users/hatim/Documents/TestImgesHotal/hotels/a/exterior.jpg"));
    }
}
