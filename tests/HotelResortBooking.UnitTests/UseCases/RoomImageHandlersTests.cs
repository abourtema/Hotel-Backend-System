using HotelResortBooking.Application.Features.Management.Rooms;
using HotelResortBooking.UnitTests.Fakes;
using Xunit;

namespace HotelResortBooking.UnitTests.UseCases;

public sealed class RoomImageHandlersTests
{
    [Fact]
    public async Task AddImage_SavesFileAndReturnsPublicUrl()
    {
        var catalog = new FakeManagementCatalogRepository();
        var roomTypeId = Guid.NewGuid();
        catalog.Rooms.Add(new Application.Abstractions.Repositories.ManagedRoomDto { Id = roomTypeId, Name = "Deluxe" });
        var storage = new FakeImageStorage();
        var urls = new FakeImageUrlResolver();
        var sut = new RoomImageHandlers(catalog, storage, urls, new FakeUnitOfWork());

        await using var stream = new MemoryStream("img"u8.ToArray());
        var result = await sut.ExecuteAsync(roomTypeId, new AddRoomImageRequest
        {
            FileContent = stream,
            FileName = "bed.jpg",
            AltText = "Bed",
            IsPrimary = true
        });

        Assert.True(result.IsSuccess);
        Assert.Equal($"rooms/{roomTypeId:N}/bed.jpg", storage.SavedPaths[0]);
        Assert.StartsWith("/uploads/", result.Value.ImageUrl);
        Assert.Contains("bed.jpg", result.Value.ImageUrl);
    }

    [Fact]
    public async Task AddImage_WithoutFile_Fails()
    {
        var sut = new RoomImageHandlers(
            new FakeManagementCatalogRepository(),
            new FakeImageStorage(),
            new FakeImageUrlResolver(),
            new FakeUnitOfWork());

        var result = await sut.ExecuteAsync(Guid.NewGuid(), new AddRoomImageRequest());

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidImage", result.Error!.Code);
    }

    [Fact]
    public async Task AddImage_UnknownRoom_Fails()
    {
        var sut = new RoomImageHandlers(
            new FakeManagementCatalogRepository(),
            new FakeImageStorage(),
            new FakeImageUrlResolver(),
            new FakeUnitOfWork());

        await using var stream = new MemoryStream("img"u8.ToArray());
        var result = await sut.ExecuteAsync(Guid.NewGuid(), new AddRoomImageRequest
        {
            FileContent = stream,
            FileName = "bed.jpg"
        });

        Assert.True(result.IsFailure);
        Assert.Equal("RoomNotFound", result.Error!.Code);
    }

    [Fact]
    public async Task DeleteImage_RemovesStoredFile()
    {
        var catalog = new FakeManagementCatalogRepository();
        var roomTypeId = Guid.NewGuid();
        var imageId = Guid.NewGuid();
        catalog.Rooms.Add(new Application.Abstractions.Repositories.ManagedRoomDto
        {
            Id = roomTypeId,
            Images =
            [
                new Application.Abstractions.Repositories.ManagedImageDto
                {
                    Id = imageId,
                    ImageUrl = "rooms/abc/photo.jpg"
                }
            ]
        });
        var storage = new FakeImageStorage();
        var sut = new RoomImageHandlers(catalog, storage, new FakeImageUrlResolver(), new FakeUnitOfWork());

        var result = await sut.ExecuteAsync(roomTypeId, imageId);

        Assert.True(result.IsSuccess);
        Assert.Contains("rooms/abc/photo.jpg", storage.DeletedPaths);
    }
}
