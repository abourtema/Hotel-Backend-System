namespace HotelResortBooking.Application.Security;

public static class Permissions
{
    public const string ClaimType = "permission";

    public const string HotelsView = "Hotels.View";
    public const string HotelsCreate = "Hotels.Create";
    public const string HotelsUpdate = "Hotels.Update";
    public const string HotelsDelete = "Hotels.Delete";

    public const string RoomsView = "Rooms.View";
    public const string RoomsCreate = "Rooms.Create";
    public const string RoomsUpdate = "Rooms.Update";
    public const string RoomsDelete = "Rooms.Delete";

    public const string BookingsView = "Bookings.View";
    public const string BookingsUpdate = "Bookings.Update";
    public const string BookingsCancel = "Bookings.Cancel";

    public const string CustomersView = "Customers.View";
    public const string CustomersUpdate = "Customers.Update";

    public const string PaymentsView = "Payments.View";
    public const string PaymentsRefund = "Payments.Refund";

    public const string LoyaltyView = "Loyalty.View";
    public const string LoyaltyAdjust = "Loyalty.Adjust";

    public const string ReviewsView = "Reviews.View";
    public const string ReviewsModerate = "Reviews.Moderate";

    public const string UpgradesView = "Upgrades.View";
    public const string UpgradesCreate = "Upgrades.Create";
    public const string UpgradesUpdate = "Upgrades.Update";
    public const string UpgradesDelete = "Upgrades.Delete";

    public const string AmenitiesView = "Amenities.View";
    public const string AmenitiesCreate = "Amenities.Create";
    public const string AmenitiesUpdate = "Amenities.Update";
    public const string AmenitiesDelete = "Amenities.Delete";

    public const string ReportsView = "Reports.View";

    public const string UsersView = "Users.View";
    public const string UsersCreate = "Users.Create";
    public const string UsersUpdate = "Users.Update";
    public const string UsersDelete = "Users.Delete";
    public const string UsersManage = "Users.Manage";

    public const string RolesView = "Roles.View";
    public const string RolesManage = "Roles.Manage";

    public static readonly IReadOnlyList<string> All =
    [
        HotelsView, HotelsCreate, HotelsUpdate, HotelsDelete,
        RoomsView, RoomsCreate, RoomsUpdate, RoomsDelete,
        BookingsView, BookingsUpdate, BookingsCancel,
        CustomersView, CustomersUpdate,
        PaymentsView, PaymentsRefund,
        LoyaltyView, LoyaltyAdjust,
        ReviewsView, ReviewsModerate,
        UpgradesView, UpgradesCreate, UpgradesUpdate, UpgradesDelete,
        AmenitiesView, AmenitiesCreate, AmenitiesUpdate, AmenitiesDelete,
        ReportsView,
        UsersView, UsersCreate, UsersUpdate, UsersDelete, UsersManage,
        RolesView, RolesManage
    ];
}
