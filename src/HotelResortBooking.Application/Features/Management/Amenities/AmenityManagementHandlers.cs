using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Domain.Management;

namespace HotelResortBooking.Application.Features.Management.Amenities;

public sealed class SaveAmenityRequest
{
    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? Icon { get; init; }
}

public interface IAmenityManagementCommands
{
    Task<Result<IReadOnlyList<ManagedAmenityDto>>> ListAsync(CancellationToken cancellationToken = default);

    Task<Result<ManagedAmenityDto>> CreateAsync(SaveAmenityRequest request, CancellationToken cancellationToken = default);

    Task<Result<ManagedAmenityDto>> UpdateAsync(Guid id, SaveAmenityRequest request, CancellationToken cancellationToken = default);

    Task<Result> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    Task<Result> AssignToPropertyAsync(Guid propertyId, Guid amenityId, CancellationToken cancellationToken = default);

    Task<Result> RemoveFromPropertyAsync(Guid propertyId, Guid amenityId, CancellationToken cancellationToken = default);
}

public sealed class AmenityManagementHandlers : IAmenityManagementCommands
{
    private readonly IManagementCatalogRepository _catalog;
    private readonly IAuditLogger _audit;
    private readonly IUnitOfWork _unitOfWork;

    public AmenityManagementHandlers(
        IManagementCatalogRepository catalog,
        IAuditLogger audit,
        IUnitOfWork unitOfWork)
    {
        _catalog = catalog;
        _audit = audit;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyList<ManagedAmenityDto>>> ListAsync(CancellationToken cancellationToken = default)
        => Result.Success(await _catalog.GetAmenitiesAsync(cancellationToken));

    public async Task<Result<ManagedAmenityDto>> CreateAsync(SaveAmenityRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result.Failure<ManagedAmenityDto>(BusinessErrors.Validation("InvalidAmenity", "Amenity name is required."));
        }

        var created = await _catalog.CreateAmenityAsync(request.Name.Trim(), request.Description, request.Icon, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogEntry
        {
            Action = AuditActions.AmenityCreated,
            EntityType = "Amenity",
            EntityId = created.Id.ToString(),
            NewValues = created
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(created);
    }

    public async Task<Result<ManagedAmenityDto>> UpdateAsync(Guid id, SaveAmenityRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result.Failure<ManagedAmenityDto>(BusinessErrors.Validation("InvalidAmenity", "Amenity name is required."));
        }

        var updated = await _catalog.UpdateAmenityAsync(id, request.Name.Trim(), request.Description, request.Icon, cancellationToken);
        if (updated is null)
        {
            return Result.Failure<ManagedAmenityDto>(BusinessErrors.AmenityNotFound(id));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogEntry
        {
            Action = AuditActions.AmenityUpdated,
            EntityType = "Amenity",
            EntityId = id.ToString(),
            NewValues = updated
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(updated);
    }

    public async Task<Result> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var ok = await _catalog.SetAmenityActiveAsync(id, isActive, cancellationToken);
        if (!ok)
        {
            return Result.Failure(BusinessErrors.AmenityNotFound(id));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> AssignToPropertyAsync(Guid propertyId, Guid amenityId, CancellationToken cancellationToken = default)
    {
        var hotel = await _catalog.GetHotelAsync(propertyId, cancellationToken);
        if (hotel is null)
        {
            return Result.Failure(BusinessErrors.PropertyNotFound(propertyId));
        }

        await _catalog.AssignAmenityToPropertyAsync(propertyId, amenityId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> RemoveFromPropertyAsync(Guid propertyId, Guid amenityId, CancellationToken cancellationToken = default)
    {
        await _catalog.RemoveAmenityFromPropertyAsync(propertyId, amenityId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
