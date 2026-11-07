namespace HotelResortBooking.Application.Security;

public static class RolePermissionCatalog
{
    public static readonly Guid SuperAdminRoleId = Guid.Parse("11111111-0000-0000-0000-000000000001");
    public static readonly Guid HotelManagerRoleId = Guid.Parse("11111111-0000-0000-0000-000000000002");
    public static readonly Guid ReservationManagerRoleId = Guid.Parse("11111111-0000-0000-0000-000000000003");
    public static readonly Guid FinanceManagerRoleId = Guid.Parse("11111111-0000-0000-0000-000000000004");
    public static readonly Guid CustomerSupportRoleId = Guid.Parse("11111111-0000-0000-0000-000000000005");
    public static readonly Guid ContentManagerRoleId = Guid.Parse("11111111-0000-0000-0000-000000000006");

    public static IReadOnlyList<(Guid Id, string Name, string Description)> Roles { get; } =
    [
        (SuperAdminRoleId, Security.Roles.SuperAdmin, "Full management access"),
        (HotelManagerRoleId, Security.Roles.HotelManager, "Hotel, room, amenity, and upgrade management"),
        (ReservationManagerRoleId, Security.Roles.ReservationManager, "Booking and reservation operations"),
        (FinanceManagerRoleId, Security.Roles.FinanceManager, "Payments, refunds, and financial reports"),
        (CustomerSupportRoleId, Security.Roles.CustomerSupport, "Customer support and booking assistance"),
        (ContentManagerRoleId, Security.Roles.ContentManager, "Reviews, amenities, and content")
    ];

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> PermissionsByRole { get; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [Security.Roles.SuperAdmin] = Permissions.All,
            [Security.Roles.Admin] = Permissions.All,
            [Security.Roles.HotelManager] =
            [
                Permissions.HotelsView, Permissions.HotelsCreate, Permissions.HotelsUpdate, Permissions.HotelsDelete,
                Permissions.RoomsView, Permissions.RoomsCreate, Permissions.RoomsUpdate, Permissions.RoomsDelete,
                Permissions.AmenitiesView, Permissions.AmenitiesCreate, Permissions.AmenitiesUpdate, Permissions.AmenitiesDelete,
                Permissions.UpgradesView, Permissions.UpgradesCreate, Permissions.UpgradesUpdate, Permissions.UpgradesDelete,
                Permissions.ReportsView, Permissions.BookingsView, Permissions.ReviewsView
            ],
            [Security.Roles.ReservationManager] =
            [
                Permissions.HotelsView, Permissions.RoomsView,
                Permissions.BookingsView, Permissions.BookingsUpdate, Permissions.BookingsCancel,
                Permissions.CustomersView, Permissions.ReportsView
            ],
            [Security.Roles.FinanceManager] =
            [
                Permissions.PaymentsView, Permissions.PaymentsRefund,
                Permissions.BookingsView, Permissions.LoyaltyView, Permissions.ReportsView
            ],
            [Security.Roles.CustomerSupport] =
            [
                Permissions.CustomersView, Permissions.CustomersUpdate,
                Permissions.BookingsView, Permissions.BookingsUpdate, Permissions.BookingsCancel,
                Permissions.LoyaltyView, Permissions.ReviewsView
            ],
            [Security.Roles.ContentManager] =
            [
                Permissions.ReviewsView, Permissions.ReviewsModerate,
                Permissions.AmenitiesView, Permissions.AmenitiesCreate, Permissions.AmenitiesUpdate,
                Permissions.HotelsView, Permissions.HotelsUpdate,
                Permissions.UpgradesView
            ]
        };

    public static Guid PermissionId(string permissionName)
    {
        var index = Permissions.All.ToList().FindIndex(p =>
            string.Equals(p, permissionName, StringComparison.OrdinalIgnoreCase));
        if (index < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(permissionName), permissionName, "Unknown permission.");
        }

        return Guid.Parse($"a1000000-0000-0000-0000-{index + 1:D12}");
    }
}
