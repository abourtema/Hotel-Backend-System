using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Features.Management.Hotels;
using HotelResortBooking.UnitTests.Fakes;
using Xunit;

namespace HotelResortBooking.UnitTests.UseCases;

public sealed class HotelImageHandlersTests
{
    [Fact]
    public async Task AddImage_SavesFileAndReturnsPublicUrl()
    {
        var catalog = new FakeManagementCatalogRepository();
        var hotelId = Guid.NewGuid();
        catalog.Hotels.Add(new Application.Abstractions.Repositories.ManagedHotelDetails { Id = hotelId, Name = "Seaside" });
        var storage = new FakeImageStorage();
        var urls = new FakeImageUrlResolver();
        var sut = new HotelImageHandlers(catalog, storage, urls, new FakeUnitOfWork());

        await using var stream = new MemoryStream("img"u8.ToArray());
        var result = await sut.ExecuteAsync(hotelId, new AddHotelImageRequest
        {
            FileContent = stream,
            FileName = "front.jpg",
            AltText = "Front",
            IsPrimary = true
        });

        Assert.True(result.IsSuccess);
        Assert.Equal($"hotels/{hotelId:N}/front.jpg", storage.SavedPaths[0]);
        Assert.StartsWith("/uploads/", result.Value.ImageUrl);
        Assert.Contains("front.jpg", result.Value.ImageUrl);
    }

    [Fact]
    public async Task AddImage_WithoutFile_Fails()
    {
        var sut = new HotelImageHandlers(
            new FakeManagementCatalogRepository(),
            new FakeImageStorage(),
            new FakeImageUrlResolver(),
            new FakeUnitOfWork());

        var result = await sut.ExecuteAsync(Guid.NewGuid(), new AddHotelImageRequest());

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidImage", result.Error!.Code);
    }

    [Fact]
    public async Task DeleteImage_RemovesStoredFile()
    {
        var catalog = new FakeManagementCatalogRepository();
        var hotelId = Guid.NewGuid();
        var imageId = Guid.NewGuid();
        catalog.Hotels.Add(new Application.Abstractions.Repositories.ManagedHotelDetails
        {
            Id = hotelId,
            Images =
            [
                new Application.Abstractions.Repositories.ManagedImageDto
                {
                    Id = imageId,
                    ImageUrl = "hotels/abc/photo.jpg"
                }
            ]
        });
        var storage = new FakeImageStorage();
        var sut = new HotelImageHandlers(catalog, storage, new FakeImageUrlResolver(), new FakeUnitOfWork());

        var result = await sut.ExecuteAsync(hotelId, imageId);

        Assert.True(result.IsSuccess);
        Assert.Contains("hotels/abc/photo.jpg", storage.DeletedPaths);
    }
}
