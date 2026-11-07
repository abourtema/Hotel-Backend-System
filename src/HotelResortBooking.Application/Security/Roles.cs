namespace HotelResortBooking.Application.Security;

public static class Roles
{
    public const string User = "User";

    public const string Admin = "Admin";

    public const string SuperAdmin = "SuperAdmin";

    public const string HotelManager = "HotelManager";

    public const string ReservationManager = "ReservationManager";

    public const string FinanceManager = "FinanceManager";

    public const string CustomerSupport = "CustomerSupport";

    public const string ContentManager = "ContentManager";

    public static readonly IReadOnlyList<string> Management =
    [
        SuperAdmin,
        HotelManager,
        ReservationManager,
        FinanceManager,
        CustomerSupport,
        ContentManager
    ];

    public static bool IsPrivileged(string? role) =>
        string.Equals(role, SuperAdmin, StringComparison.OrdinalIgnoreCase)
        || string.Equals(role, Admin, StringComparison.OrdinalIgnoreCase);

    public static bool IsManagementRole(string? role) =>
        !string.IsNullOrWhiteSpace(role)
        && (IsPrivileged(role)
            || Management.Any(r => string.Equals(r, role, StringComparison.OrdinalIgnoreCase)));
}
