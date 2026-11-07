using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Security;

namespace HotelResortBooking.UnitTests.Fakes;

public sealed class FakeRolePermissionRepository : IRolePermissionRepository
{
    public Task<IReadOnlyList<ManagementRoleDto>> GetRolesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ManagementRoleDto>>([]);

    public Task<ManagementRoleDto?> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken = default)
        => Task.FromResult<ManagementRoleDto?>(null);

    public Task<ManagementRoleDto?> GetRoleByNameAsync(string name, CancellationToken cancellationToken = default)
        => Task.FromResult<ManagementRoleDto?>(null);

    public Task<IReadOnlyList<string>> GetPermissionNamesForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<string>>([]);

    public Task<IReadOnlyList<string>> GetRoleNamesForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<string>>([Roles.User]);
}
