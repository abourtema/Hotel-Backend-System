using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Security;
using HotelResortBooking.Domain.Entities;
using HotelResortBooking.Infrastructure.Configuration;
using HotelResortBooking.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HotelResortBooking.Infrastructure.Identity;

public static class ManagementAdminSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext db,
        IPasswordHasher passwordHasher,
        ManagementSeedOptions options,
        CancellationToken cancellationToken = default)
    {
        if (!options.Enabled || string.IsNullOrWhiteSpace(options.Email) || string.IsNullOrWhiteSpace(options.Password))
        {
            return;
        }

        var email = options.Email.Trim().ToLowerInvariant();
        var superAdminRole = await db.Roles
            .FirstOrDefaultAsync(r => r.Name == Roles.SuperAdmin, cancellationToken);

        if (superAdminRole is null)
        {
            return;
        }

        var user = await db.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Email = email,
                PasswordHash = passwordHasher.Hash(options.Password),
                FirstName = string.IsNullOrWhiteSpace(options.FirstName) ? "System" : options.FirstName.Trim(),
                LastName = string.IsNullOrWhiteSpace(options.LastName) ? "Admin" : options.LastName.Trim(),
                Role = Roles.SuperAdmin,
                IsActive = true,
                IsEmailVerified = true,
                Profile = new UserProfile(),
                Preferences = new UserPreferences(),
                LoyaltyAccount = new LoyaltyAccount()
            };

            user.UserRoles.Add(new UserRole
            {
                RoleId = superAdminRole.Id,
                AssignedAt = DateTimeOffset.UtcNow
            });

            db.Users.Add(user);
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var changed = false;
        if (!user.IsActive)
        {
            user.IsActive = true;
            changed = true;
        }

        if (!string.Equals(user.Role, Roles.SuperAdmin, StringComparison.OrdinalIgnoreCase))
        {
            user.Role = Roles.SuperAdmin;
            changed = true;
        }

        if (user.UserRoles.All(ur => ur.RoleId != superAdminRole.Id))
        {
            user.UserRoles.Add(new UserRole
            {
                UserId = user.Id,
                RoleId = superAdminRole.Id,
                AssignedAt = DateTimeOffset.UtcNow
            });
            changed = true;
        }

        if (changed)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
