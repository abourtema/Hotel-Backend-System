using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.Domain.Management;

namespace HotelResortBooking.Application.Features.Management.Upgrades;

public sealed class SaveUpgradeRequest
{
    public Guid PropertyId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public UpgradeType UpgradeType { get; init; }

    public decimal Price { get; init; }

    public string Currency { get; init; } = "USD";

    public bool IsMandatory { get; init; }

    public bool IsRecommended { get; init; }

    public bool IsActive { get; init; } = true;

    public IReadOnlyList<Guid> RoomTypeIds { get; init; } = Array.Empty<Guid>();
}

public interface IUpgradeManagementCommands
{
    Task<Result<IReadOnlyList<ManagedUpgradeDto>>> ListAsync(Guid? propertyId, CancellationToken cancellationToken = default);

    Task<Result<ManagedUpgradeDto>> CreateAsync(SaveUpgradeRequest request, CancellationToken cancellationToken = default);

    Task<Result<ManagedUpgradeDto>> UpdateAsync(Guid id, SaveUpgradeRequest request, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class UpgradeManagementHandlers : IUpgradeManagementCommands
{
    private readonly IManagementCatalogRepository _catalog;
    private readonly IAuditLogger _audit;
    private readonly IUnitOfWork _unitOfWork;

    public UpgradeManagementHandlers(
        IManagementCatalogRepository catalog,
        IAuditLogger audit,
        IUnitOfWork unitOfWork)
    {
        _catalog = catalog;
        _audit = audit;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<IReadOnlyList<ManagedUpgradeDto>>> ListAsync(
        Guid? propertyId,
        CancellationToken cancellationToken = default)
        => Result.Success(await _catalog.GetUpgradesAsync(propertyId, cancellationToken));

    public async Task<Result<ManagedUpgradeDto>> CreateAsync(SaveUpgradeRequest request, CancellationToken cancellationToken = default)
    {
        var validation = Validate(request);
        if (validation is not null)
        {
            return Result.Failure<ManagedUpgradeDto>(validation);
        }

        var created = await _catalog.CreateUpgradeAsync(ToModel(request), cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogEntry
        {
            Action = AuditActions.UpgradeCreated,
            EntityType = "UpgradeOption",
            EntityId = created.Id.ToString(),
            NewValues = created
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(created);
    }

    public async Task<Result<ManagedUpgradeDto>> UpdateAsync(Guid id, SaveUpgradeRequest request, CancellationToken cancellationToken = default)
    {
        var validation = Validate(request);
        if (validation is not null)
        {
            return Result.Failure<ManagedUpgradeDto>(validation);
        }

        var updated = await _catalog.UpdateUpgradeAsync(id, ToModel(request), cancellationToken);
        if (updated is null)
        {
            return Result.Failure<ManagedUpgradeDto>(BusinessErrors.UpgradeNotFound(id));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogEntry
        {
            Action = AuditActions.UpgradeUpdated,
            EntityType = "UpgradeOption",
            EntityId = id.ToString(),
            NewValues = updated
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(updated);
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var existing = await _catalog.GetUpgradeAsync(id, cancellationToken);
        if (existing is null)
        {
            return Result.Failure(BusinessErrors.UpgradeNotFound(id));
        }

        var deleted = await _catalog.DeleteUpgradeAsync(id, cancellationToken);
        if (!deleted)
        {
            return Result.Failure(BusinessErrors.UpgradeNotFound(id));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogEntry
        {
            Action = AuditActions.UpgradeDeleted,
            EntityType = "UpgradeOption",
            EntityId = id.ToString(),
            OldValues = existing
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private static Error? Validate(SaveUpgradeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BusinessErrors.Validation("InvalidUpgrade", "Upgrade name is required.");
        }

        if (request.Price < 0)
        {
            return BusinessErrors.Validation("InvalidPrice", "Price must be a non-negative decimal.");
        }

        if (string.IsNullOrWhiteSpace(request.Currency) || request.Currency.Trim().Length != 3)
        {
            return BusinessErrors.Validation("InvalidCurrency", "Currency must be a 3-letter code.");
        }

        return null;
    }

    private static SaveUpgradeModel ToModel(SaveUpgradeRequest request) => new()
    {
        PropertyId = request.PropertyId,
        Name = request.Name.Trim(),
        Description = request.Description,
        UpgradeType = request.UpgradeType,
        Price = request.Price,
        Currency = request.Currency.Trim().ToUpperInvariant(),
        IsMandatory = request.IsMandatory,
        IsRecommended = request.IsRecommended,
        IsActive = request.IsActive,
        RoomTypeIds = request.RoomTypeIds
    };
}
