using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.Features.Management.Rooms;

public sealed class AddRoomImageRequest
{
    public Stream? FileContent { get; init; }

    public string? FileName { get; init; }

    public string? AltText { get; init; }

    public int? SortOrder { get; init; }

    public bool IsPrimary { get; init; }
}

public interface IAddRoomImageCommand
{
    Task<Result<ManagedImageDto>> ExecuteAsync(Guid roomTypeId, AddRoomImageRequest request, CancellationToken cancellationToken = default);
}

public interface IUpdateRoomImageCommand
{
    Task<Result<ManagedImageDto>> ExecuteAsync(
        Guid roomTypeId,
        Guid imageId,
        AddRoomImageRequest request,
        CancellationToken cancellationToken = default);
}

public interface IDeleteRoomImageCommand
{
    Task<Result> ExecuteAsync(Guid roomTypeId, Guid imageId, CancellationToken cancellationToken = default);
}

public interface ISetPrimaryRoomImageCommand
{
    Task<Result> ExecuteAsync(Guid roomTypeId, Guid imageId, CancellationToken cancellationToken = default);
}

public interface IReorderRoomImagesCommand
{
    Task<Result> ExecuteAsync(Guid roomTypeId, IReadOnlyList<Guid> imageIds, CancellationToken cancellationToken = default);
}

public sealed class RoomImageHandlers :
    IAddRoomImageCommand,
    IUpdateRoomImageCommand,
    IDeleteRoomImageCommand,
    ISetPrimaryRoomImageCommand,
    IReorderRoomImagesCommand
{
    private readonly IManagementCatalogRepository _catalog;
    private readonly IImageStorage _storage;
    private readonly IImageUrlResolver _urls;
    private readonly IUnitOfWork _unitOfWork;

    public RoomImageHandlers(
        IManagementCatalogRepository catalog,
        IImageStorage storage,
        IImageUrlResolver urls,
        IUnitOfWork unitOfWork)
    {
        _catalog = catalog;
        _storage = storage;
        _urls = urls;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ManagedImageDto>> ExecuteAsync(
        Guid roomTypeId,
        AddRoomImageRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.FileContent is null || string.IsNullOrWhiteSpace(request.FileName))
        {
            return Result.Failure<ManagedImageDto>(BusinessErrors.InvalidImageFile());
        }

        var room = await _catalog.GetRoomAsync(roomTypeId, cancellationToken);
        if (room is null)
        {
            return Result.Failure<ManagedImageDto>(BusinessErrors.RoomNotFound(roomTypeId));
        }

        var saved = await _storage.SaveAsync(
            ImageStorageCategories.Rooms,
            roomTypeId,
            request.FileName,
            request.FileContent,
            cancellationToken);
        if (saved.IsFailure)
        {
            return Result.Failure<ManagedImageDto>(saved.Error!);
        }

        var image = await _catalog.AddRoomImageAsync(
            roomTypeId,
            saved.Value,
            request.AltText,
            request.SortOrder,
            request.IsPrimary,
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(image.WithPublicUrl(_urls));
    }

    public async Task<Result<ManagedImageDto>> ExecuteAsync(
        Guid roomTypeId,
        Guid imageId,
        AddRoomImageRequest request,
        CancellationToken cancellationToken = default)
    {
        var room = await _catalog.GetRoomAsync(roomTypeId, cancellationToken);
        var existing = room?.Images.FirstOrDefault(i => i.Id == imageId);
        if (room is null || existing is null)
        {
            return Result.Failure<ManagedImageDto>(BusinessErrors.ImageNotFound(imageId));
        }

        string? storedPath = null;
        if (request.FileContent is not null && !string.IsNullOrWhiteSpace(request.FileName))
        {
            var saved = await _storage.SaveAsync(
                ImageStorageCategories.Rooms,
                roomTypeId,
                request.FileName,
                request.FileContent,
                cancellationToken);
            if (saved.IsFailure)
            {
                return Result.Failure<ManagedImageDto>(saved.Error!);
            }

            storedPath = saved.Value;
        }

        var updated = await _catalog.UpdateRoomImageAsync(
            roomTypeId,
            imageId,
            storedPath,
            request.AltText,
            request.SortOrder,
            request.IsPrimary ? true : null,
            cancellationToken);

        if (updated is null)
        {
            if (storedPath is not null)
            {
                await _storage.DeleteAsync(storedPath, cancellationToken);
            }

            return Result.Failure<ManagedImageDto>(BusinessErrors.ImageNotFound(imageId));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        if (storedPath is not null)
        {
            await _storage.DeleteAsync(existing.ImageUrl, cancellationToken);
        }

        return Result.Success(updated.WithPublicUrl(_urls));
    }

    public async Task<Result> ExecuteAsync(Guid roomTypeId, Guid imageId, CancellationToken cancellationToken = default)
    {
        var storedPath = await _catalog.DeleteRoomImageAsync(roomTypeId, imageId, cancellationToken);
        if (storedPath is null)
        {
            return Result.Failure(BusinessErrors.ImageNotFound(imageId));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _storage.DeleteAsync(storedPath, cancellationToken);
        return Result.Success();
    }

    async Task<Result> ISetPrimaryRoomImageCommand.ExecuteAsync(Guid roomTypeId, Guid imageId, CancellationToken cancellationToken)
    {
        var ok = await _catalog.SetPrimaryRoomImageAsync(roomTypeId, imageId, cancellationToken);
        if (!ok)
        {
            return Result.Failure(BusinessErrors.ImageNotFound(imageId));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ExecuteAsync(Guid roomTypeId, IReadOnlyList<Guid> imageIds, CancellationToken cancellationToken = default)
    {
        var room = await _catalog.GetRoomAsync(roomTypeId, cancellationToken);
        if (room is null)
        {
            return Result.Failure(BusinessErrors.RoomNotFound(roomTypeId));
        }

        await _catalog.ReorderRoomImagesAsync(roomTypeId, imageIds, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
