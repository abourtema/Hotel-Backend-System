using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Paging;
using HotelResortBooking.Application.Security;
using HotelResortBooking.Domain.Entities;
using HotelResortBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelResortBooking.Infrastructure.Repositories;

public sealed class RolePermissionRepository : IRolePermissionRepository
{
    private readonly ApplicationDbContext _db;

    public RolePermissionRepository(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<ManagementRoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
    {
        var roles = await _db.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return roles.Select(Map).ToList();
    }

    public async Task<ManagementRoleDto?> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);

        return role is null ? null : Map(role);
    }

    public async Task<ManagementRoleDto?> GetRoleByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var role = await _db.Roles
            .AsNoTracking()
            .Include(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Name == name, cancellationToken);

        return role is null ? null : Map(role);
    }

    public async Task<IReadOnlyList<string>> GetPermissionNamesForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return Array.Empty<string>();
        }

        if (Roles.IsPrivileged(user.Role))
        {
            return Permissions.All;
        }

        var fromAssignments = await _db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .SelectMany(ur => ur.Role.RolePermissions.Select(rp => rp.Permission.Name))
            .Distinct()
            .ToListAsync(cancellationToken);

        if (RolePermissionCatalog.PermissionsByRole.TryGetValue(user.Role, out var fromPrimary))
        {
            return fromAssignments.Union(fromPrimary, StringComparer.OrdinalIgnoreCase).ToList();
        }

        return fromAssignments;
    }

    public async Task<IReadOnlyList<string>> GetRoleNamesForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return Array.Empty<string>();
        }

        var names = await _db.UserRoles
            .AsNoTracking()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Name)
            .ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(user.Role) && !names.Contains(user.Role, StringComparer.OrdinalIgnoreCase))
        {
            names.Insert(0, user.Role);
        }

        return names;
    }

    private static ManagementRoleDto Map(Role role) => new()
    {
        Id = role.Id,
        Name = role.Name,
        Description = role.Description,
        Permissions = role.RolePermissions.Select(rp => rp.Permission.Name).OrderBy(n => n).ToList()
    };
}

public sealed class ManagementUserRepository : IManagementUserRepository
{
    private readonly ApplicationDbContext _db;

    public ManagementUserRepository(ApplicationDbContext db) => _db = db;

    public async Task<PagedResult<ManagementUserListItem>> SearchAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = ManagementUsers();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = $"%{search.Trim()}%";
            query = query.Where(u =>
                EF.Functions.Like(u.Email, term)
                || EF.Functions.Like(u.FirstName, term)
                || EF.Functions.Like(u.LastName, term));
        }

        var total = await query.CountAsync(cancellationToken);
        var users = await query
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .ToListAsync(cancellationToken);

        return new PagedResult<ManagementUserListItem>
        {
            Items = users.Select(Map).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<ManagementUserListItem?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r.RolePermissions)
            .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        return user is null ? null : Map(user);
    }

    public async Task<ManagementUserListItem> CreateAsync(
        CreateManagementUserModel model,
        CancellationToken cancellationToken = default)
    {
        var roleName = Roles.HotelManager;
        Role? role = null;
        if (model.RoleId is Guid roleId)
        {
            role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken);
            if (role is not null)
            {
                roleName = role.Name;
            }
        }

        var user = new User
        {
            Email = model.Email.Trim().ToLowerInvariant(),
            PasswordHash = model.PasswordHash,
            FirstName = model.FirstName,
            LastName = model.LastName,
            PhoneNumber = model.PhoneNumber,
            IsActive = true,
            Role = roleName,
            Profile = new UserProfile(),
            Preferences = new UserPreferences(),
            LoyaltyAccount = new LoyaltyAccount()
        };

        if (role is not null)
        {
            user.UserRoles.Add(new UserRole
            {
                RoleId = role.Id,
                AssignedAt = DateTimeOffset.UtcNow
            });
        }

        _db.Users.Add(user);
        await Task.CompletedTask;
        return Map(user);
    }

    public async Task UpdateAsync(
        Guid userId,
        string firstName,
        string lastName,
        string? phoneNumber,
        CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");

        user.FirstName = firstName;
        user.LastName = lastName;
        user.PhoneNumber = phoneNumber;
    }

    public async Task SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");

        user.IsActive = isActive;
    }

    public async Task AssignRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new InvalidOperationException("User was not found.");

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Id == roleId, cancellationToken)
            ?? throw new InvalidOperationException("Role was not found.");

        if (user.UserRoles.All(ur => ur.RoleId != roleId))
        {
            user.UserRoles.Add(new UserRole
            {
                UserId = userId,
                RoleId = roleId,
                AssignedAt = DateTimeOffset.UtcNow
            });
        }

        user.Role = role.Name;
    }

    public async Task RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        var assignment = await _db.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);

        if (assignment is not null)
        {
            _db.UserRoles.Remove(assignment);
        }

        var user = await _db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            return;
        }

        var remaining = user.UserRoles.Where(ur => ur.RoleId != roleId).Select(ur => ur.Role.Name).FirstOrDefault();
        user.Role = remaining ?? Roles.User;
    }

    private IQueryable<User> ManagementUsers()
        => _db.Users.Where(u =>
            u.Role != Roles.User
            || u.UserRoles.Any());

    private static ManagementUserListItem Map(User user) => new()
    {
        Id = user.Id,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        PhoneNumber = user.PhoneNumber,
        IsActive = user.IsActive,
        PrimaryRole = user.Role,
        CreatedAt = user.CreatedAt,
        Roles = user.UserRoles.Select(ur => new ManagementRoleDto
        {
            Id = ur.Role.Id,
            Name = ur.Role.Name,
            Description = ur.Role.Description,
            Permissions = ur.Role.RolePermissions?.Select(rp => rp.Permission.Name).ToList()
                ?? new List<string>()
        }).ToList()
    };
}
