using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Paging;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Domain.Management;

namespace HotelResortBooking.Application.Features.Management.Rooms;

public sealed class SaveRoomRequest
{
    public Guid PropertyId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public decimal? RoomSize { get; init; }

    public int MaxAdults { get; init; }

    public int MaxChildren { get; init; }

    public int MaxGuests { get; init; }

    public string? BedType { get; init; }

    public decimal BasePricePerNight { get; init; }

    public string Currency { get; init; } = "USD";

    public bool IsActive { get; init; } = true;
}

public sealed class GetManagedRoomsQuery
{
    public Guid? PropertyId { get; init; }

    public bool? IsActive { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

public interface IRoomManagementCommands
{
    Task<Result<PagedResult<ManagedRoomDto>>> ListAsync(GetManagedRoomsQuery query, CancellationToken cancellationToken = default);

    Task<Result<ManagedRoomDto>> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<ManagedRoomDto>> CreateAsync(SaveRoomRequest request, CancellationToken cancellationToken = default);

    Task<Result<ManagedRoomDto>> UpdateAsync(Guid id, SaveRoomRequest request, CancellationToken cancellationToken = default);

    Task<Result> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<RoomInventoryDto>>> GetAvailabilityAsync(
        Guid roomTypeId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    Task<Result> UpdateAvailabilityAsync(
        Guid roomTypeId,
        IReadOnlyList<RoomInventoryUpdateItem> items,
        CancellationToken cancellationToken = default);
}

public sealed class RoomManagementHandlers : IRoomManagementCommands
{
    private readonly IManagementCatalogRepository _catalog;
    private readonly IImageUrlResolver _urls;
    private readonly IAuditLogger _audit;
    private readonly IUnitOfWork _unitOfWork;

    public RoomManagementHandlers(
        IManagementCatalogRepository catalog,
        IImageUrlResolver urls,
        IAuditLogger audit,
        IUnitOfWork unitOfWork)
    {
        _catalog = catalog;
        _urls = urls;
        _audit = audit;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PagedResult<ManagedRoomDto>>> ListAsync(
        GetManagedRoomsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page <= 0 || query.PageSize <= 0)
        {
            return Result.Failure<PagedResult<ManagedRoomDto>>(BusinessErrors.InvalidPagination());
        }

        var page = await _catalog.SearchRoomsAsync(query.PropertyId, query.IsActive, query.Page, Math.Min(query.PageSize, 100), cancellationToken);
        return Result.Success(new PagedResult<ManagedRoomDto>
        {
            Items = page.Items.Select(room => room.WithPublicImageUrls(_urls)).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount
        });
    }

    public async Task<Result<ManagedRoomDto>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var room = await _catalog.GetRoomAsync(id, cancellationToken);
        return room is null
            ? Result.Failure<ManagedRoomDto>(BusinessErrors.RoomNotFound(id))
            : Result.Success(room.WithPublicImageUrls(_urls));
    }

    public async Task<Result<ManagedRoomDto>> CreateAsync(SaveRoomRequest request, CancellationToken cancellationToken = default)
    {
        var validation = Validate(request);
        if (validation is not null)
        {
            return Result.Failure<ManagedRoomDto>(validation);
        }

        var hotel = await _catalog.GetHotelAsync(request.PropertyId, cancellationToken);
        if (hotel is null)
        {
            return Result.Failure<ManagedRoomDto>(BusinessErrors.PropertyNotFound(request.PropertyId));
        }

        var created = await _catalog.CreateRoomAsync(ToModel(request), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogEntry
        {
            Action = AuditActions.RoomCreated,
            EntityType = "RoomType",
            EntityId = created.Id.ToString(),
            NewValues = created
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(created.WithPublicImageUrls(_urls));
    }

    public async Task<Result<ManagedRoomDto>> UpdateAsync(Guid id, SaveRoomRequest request, CancellationToken cancellationToken = default)
    {
        var validation = Validate(request);
        if (validation is not null)
        {
            return Result.Failure<ManagedRoomDto>(validation);
        }

        var existing = await _catalog.GetRoomAsync(id, cancellationToken);
        if (existing is null)
        {
            return Result.Failure<ManagedRoomDto>(BusinessErrors.RoomNotFound(id));
        }

        var updated = await _catalog.UpdateRoomAsync(id, ToModel(request), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var action = existing.BasePricePerNight != request.BasePricePerNight
            ? AuditActions.RoomPriceChanged
            : AuditActions.RoomUpdated;
        await _audit.LogAsync(new AuditLogEntry
        {
            Action = action,
            EntityType = "RoomType",
            EntityId = id.ToString(),
            OldValues = existing,
            NewValues = updated
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(updated!.WithPublicImageUrls(_urls));
    }

    public async Task<Result> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var existing = await _catalog.GetRoomAsync(id, cancellationToken);
        if (existing is null)
        {
            return Result.Failure(BusinessErrors.RoomNotFound(id));
        }

        await _catalog.SetRoomActiveAsync(id, isActive, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogEntry
        {
            Action = isActive ? AuditActions.RoomActivated : AuditActions.RoomDeactivated,
            EntityType = "RoomType",
            EntityId = id.ToString(),
            NewValues = new { IsActive = isActive }
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<IReadOnlyList<RoomInventoryDto>>> GetAvailabilityAsync(
        Guid roomTypeId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default)
    {
        if (to < from)
        {
            return Result.Failure<IReadOnlyList<RoomInventoryDto>>(BusinessErrors.InvalidDateRange());
        }

        var room = await _catalog.GetRoomAsync(roomTypeId, cancellationToken);
        if (room is null)
        {
            return Result.Failure<IReadOnlyList<RoomInventoryDto>>(BusinessErrors.RoomNotFound(roomTypeId));
        }

        var items = await _catalog.GetInventoryAsync(roomTypeId, from, to, cancellationToken);
        return Result.Success(items);
    }

    public async Task<Result> UpdateAvailabilityAsync(
        Guid roomTypeId,
        IReadOnlyList<RoomInventoryUpdateItem> items,
        CancellationToken cancellationToken = default)
    {
        var room = await _catalog.GetRoomAsync(roomTypeId, cancellationToken);
        if (room is null)
        {
            return Result.Failure(BusinessErrors.RoomNotFound(roomTypeId));
        }

        if (items.Any(i => i.AvailableRooms < 0 || i.TotalRooms < 0 || i.AvailableRooms > i.TotalRooms))
        {
            return Result.Failure(BusinessErrors.InvalidInventory());
        }

        await _catalog.UpsertInventoryAsync(roomTypeId, items, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static Error? Validate(SaveRoomRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BusinessErrors.Validation("InvalidRoom", "Room name is required.");
        }

        if (request.MaxGuests <= 0 || request.MaxAdults < 0 || request.MaxChildren < 0)
        {
            return BusinessErrors.InvalidOccupancy();
        }

        if (request.BasePricePerNight < 0)
        {
            return BusinessErrors.Validation("InvalidPrice", "Price must be a non-negative decimal.");
        }

        if (string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Trim().Length != 3)
        {
            return BusinessErrors.Validation("InvalidCurrency", "Currency must be a 3-letter code.");
        }

        if (request.RoomSize is <= 0)
        {
            return BusinessErrors.Validation("InvalidRoomSize", "Room size must be greater than zero.");
        }

        return null;
    }

    private static SaveRoomTypeModel ToModel(SaveRoomRequest request) => new()
    {
        PropertyId = request.PropertyId,
        Name = request.Name.Trim(),
        Description = request.Description?.Trim() ?? string.Empty,
        RoomSize = request.RoomSize,
        MaxAdults = request.MaxAdults,
        MaxChildren = request.MaxChildren,
        MaxGuests = request.MaxGuests,
        BedType = request.BedType,
        BasePricePerNight = request.BasePricePerNight,
        Currency = request.Currency.Trim().ToUpperInvariant(),
        IsActive = request.IsActive
    };
}
