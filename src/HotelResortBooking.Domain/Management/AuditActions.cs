namespace HotelResortBooking.Domain.Management;

public static class AuditActions
{
    public const string HotelCreated = "HotelCreated";
    public const string HotelUpdated = "HotelUpdated";
    public const string HotelDeactivated = "HotelDeactivated";
    public const string HotelActivated = "HotelActivated";

    public const string RoomCreated = "RoomCreated";
    public const string RoomUpdated = "RoomUpdated";
    public const string RoomPriceChanged = "RoomPriceChanged";
    public const string RoomDeactivated = "RoomDeactivated";
    public const string RoomActivated = "RoomActivated";

    public const string BookingCancelled = "BookingCancelled";
    public const string BookingModified = "BookingModified";
    public const string BookingConfirmed = "BookingConfirmed";
    public const string BookingCompleted = "BookingCompleted";

    public const string PaymentRefunded = "PaymentRefunded";
    public const string PaymentConfirmed = "PaymentConfirmed";

    public const string LoyaltyPointsAdjusted = "LoyaltyPointsAdjusted";

    public const string ReviewApproved = "ReviewApproved";
    public const string ReviewRejected = "ReviewRejected";
    public const string ReviewHidden = "ReviewHidden";

    public const string RoleAssigned = "RoleAssigned";
    public const string RoleRemoved = "RoleRemoved";

    public const string UserActivated = "UserActivated";
    public const string UserDeactivated = "UserDeactivated";
    public const string UserCreated = "UserCreated";
    public const string UserUpdated = "UserUpdated";

    public const string AmenityCreated = "AmenityCreated";
    public const string AmenityUpdated = "AmenityUpdated";
    public const string UpgradeCreated = "UpgradeCreated";
    public const string UpgradeUpdated = "UpgradeUpdated";
    public const string UpgradeDeleted = "UpgradeDeleted";
}
