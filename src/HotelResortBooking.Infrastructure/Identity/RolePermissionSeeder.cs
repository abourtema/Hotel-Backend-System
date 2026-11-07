using HotelResortBooking.Application.Security;
using HotelResortBooking.Domain.Entities;
using HotelResortBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelResortBooking.Infrastructure.Identity;

public static class RolePermissionSeeder
{
    public static async Task SeedAsync(ApplicationDbContext db, CancellationToken cancellationToken = default)
    {
        var existingPermissions = await db.Permissions.ToListAsync(cancellationToken);
        foreach (var name in Permissions.All)
        {
            if (existingPermissions.Any(p => p.Name == name))
            {
                continue;
            }

            db.Permissions.Add(new Permission
            {
                Id = RolePermissionCatalog.PermissionId(name),
                Name = name,
                Description = name
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        var permissions = await db.Permissions.ToListAsync(cancellationToken);
        var existingRoles = await db.Roles.Include(r => r.RolePermissions).ToListAsync(cancellationToken);

        foreach (var (id, name, description) in RolePermissionCatalog.Roles)
        {
            var role = existingRoles.FirstOrDefault(r => r.Id == id || r.Name == name);
            if (role is null)
            {
                role = new Role
                {
                    Id = id,
                    Name = name,
                    Description = description,
                    IsSystem = true
                };
                db.Roles.Add(role);
                existingRoles.Add(role);
            }

            if (!RolePermissionCatalog.PermissionsByRole.TryGetValue(name, out var granted))
            {
                continue;
            }

            foreach (var permissionName in granted)
            {
                var permission = permissions.First(p => p.Name == permissionName);
                if (role.RolePermissions.Any(rp => rp.PermissionId == permission.Id))
                {
                    continue;
                }

                role.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permission.Id
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
