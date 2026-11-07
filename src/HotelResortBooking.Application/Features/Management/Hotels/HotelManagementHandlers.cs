using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Paging;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.Domain.Management;

namespace HotelResortBooking.Application.Features.Management.Hotels;

public sealed class SaveHotelRequest
{
    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public PropertyType PropertyType { get; init; }

    public int StarRating { get; init; }

    public string Address { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public TimeOnly CheckInTime { get; init; }

    public TimeOnly CheckOutTime { get; init; }

    public string? CancellationPolicy { get; init; }

    public string? DepositPolicy { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class GetHotelsForManagementQuery
{
    public string? Search { get; init; }

    public bool? IsActive { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

public interface IGetHotelsForManagementQuery
{
    Task<Result<PagedResult<ManagedHotelListItem>>> ExecuteAsync(
        GetHotelsForManagementQuery query,
        CancellationToken cancellationToken = default);
}

public interface IGetHotelForManagementQuery
{
    Task<Result<ManagedHotelDetails>> ExecuteAsync(Guid hotelId, CancellationToken cancellationToken = default);
}

public interface ICreateHotelCommand
{
    Task<Result<ManagedHotelDetails>> ExecuteAsync(SaveHotelRequest request, CancellationToken cancellationToken = default);
}

public interface IUpdateHotelCommand
{
    Task<Result<ManagedHotelDetails>> ExecuteAsync(Guid hotelId, SaveHotelRequest request, CancellationToken cancellationToken = default);
}

public interface ISetHotelActiveCommand
{
    Task<Result> ExecuteAsync(Guid hotelId, bool isActive, CancellationToken cancellationToken = default);
}

public sealed class GetHotelsForManagementHandler : IGetHotelsForManagementQuery
{
    private readonly IManagementCatalogRepository _catalog;

    public GetHotelsForManagementHandler(IManagementCatalogRepository catalog) => _catalog = catalog;

    public async Task<Result<PagedResult<ManagedHotelListItem>>> ExecuteAsync(
        GetHotelsForManagementQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page <= 0 || query.PageSize <= 0)
        {
            return Result.Failure<PagedResult<ManagedHotelListItem>>(BusinessErrors.InvalidPagination());
        }

        var page = await _catalog.SearchHotelsAsync(query.Search, query.IsActive, query.Page, Math.Min(query.PageSize, 100), cancellationToken);
        return Result.Success(page);
    }
}

public sealed class GetHotelForManagementHandler : IGetHotelForManagementQuery
{
    private readonly IManagementCatalogRepository _catalog;
    private readonly IImageUrlResolver _urls;

    public GetHotelForManagementHandler(IManagementCatalogRepository catalog, IImageUrlResolver urls)
    {
        _catalog = catalog;
        _urls = urls;
    }

    public async Task<Result<ManagedHotelDetails>> ExecuteAsync(Guid hotelId, CancellationToken cancellationToken = default)
    {
        var hotel = await _catalog.GetHotelAsync(hotelId, cancellationToken);
        return hotel is null
            ? Result.Failure<ManagedHotelDetails>(BusinessErrors.PropertyNotFound(hotelId))
            : Result.Success(hotel.WithPublicImageUrls(_urls));
    }
}

public sealed class CreateHotelHandler : ICreateHotelCommand
{
    private readonly IManagementCatalogRepository _catalog;
    private readonly IAuditLogger _audit;
    private readonly IUnitOfWork _unitOfWork;

    public CreateHotelHandler(IManagementCatalogRepository catalog, IAuditLogger audit, IUnitOfWork unitOfWork)
    {
        _catalog = catalog;
        _audit = audit;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ManagedHotelDetails>> ExecuteAsync(SaveHotelRequest request, CancellationToken cancellationToken = default)
    {
        var validation = HotelManagementValidator.Validate(request);
        if (validation is not null)
        {
            return Result.Failure<ManagedHotelDetails>(validation);
        }

        var created = await _catalog.CreateHotelAsync(HotelManagementValidator.ToModel(request), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogEntry
        {
            Action = AuditActions.HotelCreated,
            EntityType = "Property",
            EntityId = created.Id.ToString(),
            NewValues = created
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(created);
    }
}

public sealed class UpdateHotelHandler : IUpdateHotelCommand
{
    private readonly IManagementCatalogRepository _catalog;
    private readonly IAuditLogger _audit;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateHotelHandler(IManagementCatalogRepository catalog, IAuditLogger audit, IUnitOfWork unitOfWork)
    {
        _catalog = catalog;
        _audit = audit;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ManagedHotelDetails>> ExecuteAsync(
        Guid hotelId,
        SaveHotelRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = HotelManagementValidator.Validate(request);
        if (validation is not null)
        {
            return Result.Failure<ManagedHotelDetails>(validation);
        }

        var existing = await _catalog.GetHotelAsync(hotelId, cancellationToken);
        if (existing is null)
        {
            return Result.Failure<ManagedHotelDetails>(BusinessErrors.PropertyNotFound(hotelId));
        }

        var updated = await _catalog.UpdateHotelAsync(hotelId, HotelManagementValidator.ToModel(request), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogEntry
        {
            Action = AuditActions.HotelUpdated,
            EntityType = "Property",
            EntityId = hotelId.ToString(),
            OldValues = existing,
            NewValues = updated
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(updated!);
    }
}

public sealed class SetHotelActiveHandler : ISetHotelActiveCommand
{
    private readonly IManagementCatalogRepository _catalog;
    private readonly IAuditLogger _audit;
    private readonly IUnitOfWork _unitOfWork;

    public SetHotelActiveHandler(IManagementCatalogRepository catalog, IAuditLogger audit, IUnitOfWork unitOfWork)
    {
        _catalog = catalog;
        _audit = audit;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> ExecuteAsync(Guid hotelId, bool isActive, CancellationToken cancellationToken = default)
    {
        var existing = await _catalog.GetHotelAsync(hotelId, cancellationToken);
        if (existing is null)
        {
            return Result.Failure(BusinessErrors.PropertyNotFound(hotelId));
        }

        await _catalog.SetHotelActiveAsync(hotelId, isActive, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogEntry
        {
            Action = isActive ? AuditActions.HotelActivated : AuditActions.HotelDeactivated,
            EntityType = "Property",
            EntityId = hotelId.ToString(),
            OldValues = new { existing.IsActive },
            NewValues = new { IsActive = isActive }
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

internal static class HotelManagementValidator
{
    public static Error? Validate(SaveHotelRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BusinessErrors.Validation("InvalidHotel", "Hotel name is required.");
        }

        if (request.StarRating is < 0 or > 5)
        {
            return BusinessErrors.Validation("InvalidStarRating", "Star rating must be between 0 and 5.");
        }

        if (request.Latitude is < -90 or > 90 || request.Longitude is < -180 or > 180)
        {
            return BusinessErrors.InvalidLocation();
        }

        if (string.IsNullOrWhiteSpace(request.City) || string.IsNullOrWhiteSpace(request.Country))
        {
            return BusinessErrors.Validation("InvalidHotel", "City and country are required.");
        }

        return null;
    }

    public static SaveHotelModel ToModel(SaveHotelRequest request) => new()
    {
        Name = request.Name.Trim(),
        Description = request.Description?.Trim() ?? string.Empty,
        PropertyType = request.PropertyType,
        StarRating = request.StarRating,
        AddressLine = request.Address.Trim(),
        City = request.City.Trim(),
        Country = request.Country.Trim(),
        Latitude = request.Latitude,
        Longitude = request.Longitude,
        CheckInTime = request.CheckInTime,
        CheckOutTime = request.CheckOutTime,
        CancellationPolicy = request.CancellationPolicy,
        DepositPolicy = request.DepositPolicy,
        IsActive = request.IsActive
    };
}
