namespace HotelResortBooking.Application.Security;

public static class PermissionEvaluator
{
    public static bool IsAllowed(
        IEnumerable<string> roles,
        IEnumerable<string> permissions,
        string requiredPermission)
    {
        if (string.IsNullOrWhiteSpace(requiredPermission))
        {
            return false;
        }

        if (roles.Any(Roles.IsPrivileged))
        {
            return true;
        }

        return permissions.Any(p => string.Equals(p, requiredPermission, StringComparison.OrdinalIgnoreCase));
    }
}
