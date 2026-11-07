using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.Features.Management.Hotels;

public sealed class AddHotelImageRequest
{
    public Stream? FileContent { get; init; }

    public string? FileName { get; init; }

    public string? AltText { get; init; }

    public int? SortOrder { get; init; }

    public bool IsPrimary { get; init; }
}

public interface IAddHotelImageCommand
{
    Task<Result<ManagedImageDto>> ExecuteAsync(Guid hotelId, AddHotelImageRequest request, CancellationToken cancellationToken = default);
}

public interface IUpdateHotelImageCommand
{
    Task<Result<ManagedImageDto>> ExecuteAsync(
        Guid hotelId,
        Guid imageId,
        AddHotelImageRequest request,
        CancellationToken cancellationToken = default);
}

public interface IDeleteHotelImageCommand
{
    Task<Result> ExecuteAsync(Guid hotelId, Guid imageId, CancellationToken cancellationToken = default);
}

public interface ISetPrimaryHotelImageCommand
{
    Task<Result> ExecuteAsync(Guid hotelId, Guid imageId, CancellationToken cancellationToken = default);
}

public interface IReorderHotelImagesCommand
{
    Task<Result> ExecuteAsync(Guid hotelId, IReadOnlyList<Guid> imageIds, CancellationToken cancellationToken = default);
}

public sealed class HotelImageHandlers :
    IAddHotelImageCommand,
    IUpdateHotelImageCommand,
    IDeleteHotelImageCommand,
    ISetPrimaryHotelImageCommand,
    IReorderHotelImagesCommand
{
    private readonly IManagementCatalogRepository _catalog;
    private readonly IImageStorage _storage;
    private readonly IImageUrlResolver _urls;
    private readonly IUnitOfWork _unitOfWork;

    public HotelImageHandlers(
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
        Guid hotelId,
        AddHotelImageRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.FileContent is null || string.IsNullOrWhiteSpace(request.FileName))
        {
            return Result.Failure<ManagedImageDto>(BusinessErrors.InvalidImageFile());
        }

        var hotel = await _catalog.GetHotelAsync(hotelId, cancellationToken);
        if (hotel is null)
        {
            return Result.Failure<ManagedImageDto>(BusinessErrors.PropertyNotFound(hotelId));
        }

        var saved = await _storage.SaveAsync(
            ImageStorageCategories.Hotels,
            hotelId,
            request.FileName,
            request.FileContent,
            cancellationToken);
        if (saved.IsFailure)
        {
            return Result.Failure<ManagedImageDto>(saved.Error!);
        }

        var image = await _catalog.AddHotelImageAsync(
            hotelId,
            saved.Value,
            request.AltText,
            request.SortOrder,
            request.IsPrimary,
            cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(image.WithPublicUrl(_urls));
    }

    public async Task<Result<ManagedImageDto>> ExecuteAsync(
        Guid hotelId,
        Guid imageId,
        AddHotelImageRequest request,
        CancellationToken cancellationToken = default)
    {
        var hotel = await _catalog.GetHotelAsync(hotelId, cancellationToken);
        var existing = hotel?.Images.FirstOrDefault(i => i.Id == imageId);
        if (hotel is null || existing is null)
        {
            return Result.Failure<ManagedImageDto>(BusinessErrors.ImageNotFound(imageId));
        }

        string? storedPath = null;
        if (request.FileContent is not null && !string.IsNullOrWhiteSpace(request.FileName))
        {
            var saved = await _storage.SaveAsync(
                ImageStorageCategories.Hotels,
                hotelId,
                request.FileName,
                request.FileContent,
                cancellationToken);
            if (saved.IsFailure)
            {
                return Result.Failure<ManagedImageDto>(saved.Error!);
            }

            storedPath = saved.Value;
        }

        var updated = await _catalog.UpdateHotelImageAsync(
            hotelId,
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

    public async Task<Result> ExecuteAsync(Guid hotelId, Guid imageId, CancellationToken cancellationToken = default)
    {
        var storedPath = await _catalog.DeleteHotelImageAsync(hotelId, imageId, cancellationToken);
        if (storedPath is null)
        {
            return Result.Failure(BusinessErrors.ImageNotFound(imageId));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _storage.DeleteAsync(storedPath, cancellationToken);
        return Result.Success();
    }

    async Task<Result> ISetPrimaryHotelImageCommand.ExecuteAsync(Guid hotelId, Guid imageId, CancellationToken cancellationToken)
    {
        var ok = await _catalog.SetPrimaryHotelImageAsync(hotelId, imageId, cancellationToken);
        if (!ok)
        {
            return Result.Failure(BusinessErrors.ImageNotFound(imageId));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> ExecuteAsync(Guid hotelId, IReadOnlyList<Guid> imageIds, CancellationToken cancellationToken = default)
    {
        var hotel = await _catalog.GetHotelAsync(hotelId, cancellationToken);
        if (hotel is null)
        {
            return Result.Failure(BusinessErrors.PropertyNotFound(hotelId));
        }

        await _catalog.ReorderHotelImagesAsync(hotelId, imageIds, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
