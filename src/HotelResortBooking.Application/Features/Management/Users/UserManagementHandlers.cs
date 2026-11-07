using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Paging;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Domain.Management;

namespace HotelResortBooking.Application.Features.Management.Users;

public sealed class CreateManagementUserRequest
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string? PhoneNumber { get; init; }

    public Guid? RoleId { get; init; }
}

public sealed class UpdateManagementUserRequest
{
    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string? PhoneNumber { get; init; }
}

public sealed class AssignRoleRequest
{
    public Guid RoleId { get; init; }
}

public interface IManagementUserCommands
{
    Task<Result<PagedResult<ManagementUserListItem>>> ListAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Result<ManagementUserListItem>> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<ManagementUserListItem>> CreateAsync(
        CreateManagementUserRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<ManagementUserListItem>> UpdateAsync(
        Guid id,
        UpdateManagementUserRequest request,
        CancellationToken cancellationToken = default);

    Task<Result> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    Task<Result> AssignRoleAsync(Guid id, Guid roleId, CancellationToken cancellationToken = default);

    Task<Result> RemoveRoleAsync(Guid id, Guid roleId, CancellationToken cancellationToken = default);
}

public sealed class ManagementUserHandlers : IManagementUserCommands
{
    private readonly IManagementUserRepository _users;
    private readonly IUserRepository _accounts;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAuditLogger _audit;
    private readonly IUnitOfWork _unitOfWork;

    public ManagementUserHandlers(
        IManagementUserRepository users,
        IUserRepository accounts,
        IPasswordHasher passwordHasher,
        IAuditLogger audit,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _accounts = accounts;
        _passwordHasher = passwordHasher;
        _audit = audit;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PagedResult<ManagementUserListItem>>> ListAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (page <= 0 || pageSize <= 0)
        {
            return Result.Failure<PagedResult<ManagementUserListItem>>(BusinessErrors.InvalidPagination());
        }

        var result = await _users.SearchAsync(search, page, Math.Min(pageSize, 100), cancellationToken);
        return Result.Success(result);
    }

    public async Task<Result<ManagementUserListItem>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(id, cancellationToken);
        return user is null
            ? Result.Failure<ManagementUserListItem>(BusinessErrors.UserNotFound())
            : Result.Success(user);
    }

    public async Task<Result<ManagementUserListItem>> CreateAsync(
        CreateManagementUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
        {
            return Result.Failure<ManagementUserListItem>(BusinessErrors.InvalidEmail());
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return Result.Failure<ManagementUserListItem>(BusinessErrors.WeakPassword());
        }

        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return Result.Failure<ManagementUserListItem>(
                BusinessErrors.Validation("InvalidUser", "First name and last name are required."));
        }

        if (await _accounts.EmailExistsAsync(email, cancellationToken))
        {
            return Result.Failure<ManagementUserListItem>(BusinessErrors.EmailAlreadyRegistered());
        }

        var created = await _users.CreateAsync(
            new CreateManagementUserModel
            {
                Email = email,
                PasswordHash = _passwordHasher.Hash(request.Password),
                FirstName = request.FirstName.Trim(),
                LastName = request.LastName.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber) ? null : request.PhoneNumber.Trim(),
                RoleId = request.RoleId
            },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogEntry
        {
            Action = AuditActions.UserCreated,
            EntityType = "User",
            EntityId = created.Id.ToString(),
            NewValues = new { created.Id, created.Email, created.PrimaryRole }
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(created);
    }

    public async Task<Result<ManagementUserListItem>> UpdateAsync(
        Guid id,
        UpdateManagementUserRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await _users.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return Result.Failure<ManagementUserListItem>(BusinessErrors.UserNotFound());
        }

        if (string.IsNullOrWhiteSpace(request.FirstName) || string.IsNullOrWhiteSpace(request.LastName))
        {
            return Result.Failure<ManagementUserListItem>(
                BusinessErrors.Validation("InvalidUser", "First name and last name are required."));
        }

        await _users.UpdateAsync(id, request.FirstName.Trim(), request.LastName.Trim(), request.PhoneNumber, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogEntry
        {
            Action = AuditActions.UserUpdated,
            EntityType = "User",
            EntityId = id.ToString(),
            OldValues = new { existing.FirstName, existing.LastName, existing.PhoneNumber },
            NewValues = new { request.FirstName, request.LastName, request.PhoneNumber }
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await _users.GetByIdAsync(id, cancellationToken);
        return Result.Success(updated!);
    }

    public async Task<Result> SetActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default)
    {
        var existing = await _users.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return Result.Failure(BusinessErrors.UserNotFound());
        }

        await _users.SetActiveAsync(id, isActive, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogEntry
        {
            Action = isActive ? AuditActions.UserActivated : AuditActions.UserDeactivated,
            EntityType = "User",
            EntityId = id.ToString(),
            NewValues = new { IsActive = isActive }
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> AssignRoleAsync(Guid id, Guid roleId, CancellationToken cancellationToken = default)
    {
        var existing = await _users.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return Result.Failure(BusinessErrors.UserNotFound());
        }

        await _users.AssignRoleAsync(id, roleId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogEntry
        {
            Action = AuditActions.RoleAssigned,
            EntityType = "UserRole",
            EntityId = id.ToString(),
            NewValues = new { UserId = id, RoleId = roleId }
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> RemoveRoleAsync(Guid id, Guid roleId, CancellationToken cancellationToken = default)
    {
        var existing = await _users.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return Result.Failure(BusinessErrors.UserNotFound());
        }

        await _users.RemoveRoleAsync(id, roleId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogEntry
        {
            Action = AuditActions.RoleRemoved,
            EntityType = "UserRole",
            EntityId = id.ToString(),
            OldValues = new { UserId = id, RoleId = roleId }
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public interface IRoleManagementQueries
{
    Task<Result<IReadOnlyList<ManagementRoleDto>>> ListAsync(CancellationToken cancellationToken = default);

    Task<Result<ManagementRoleDto>> GetAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class RoleManagementHandlers : IRoleManagementQueries
{
    private readonly IRolePermissionRepository _roles;

    public RoleManagementHandlers(IRolePermissionRepository roles) => _roles = roles;

    public async Task<Result<IReadOnlyList<ManagementRoleDto>>> ListAsync(CancellationToken cancellationToken = default)
        => Result.Success(await _roles.GetRolesAsync(cancellationToken));

    public async Task<Result<ManagementRoleDto>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await _roles.GetRoleByIdAsync(id, cancellationToken);
        return role is null
            ? Result.Failure<ManagementRoleDto>(BusinessErrors.RoleNotFound())
            : Result.Success(role);
    }
}
