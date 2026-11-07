using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.Common.Errors;

public static class BusinessErrors
{
    public static Error InvalidDateRange()
        => Error.Validation("InvalidDateRange", "Check-in date must be before check-out date.");

    public static Error InvalidGuestCount()
        => Error.Validation("InvalidGuestCount", "At least one adult guest is required.");

    public static Error InvalidGuestDetails()
        => Error.Validation(
            "InvalidGuestDetails",
            "Primary guest first name, last name, email, and phone number are required.");

    public static Error InvalidSpecialRequests()
        => Error.Validation("InvalidSpecialRequests", "Special requests cannot exceed 500 characters.");

    public static Error UnsupportedCheckoutPaymentMethod()
        => Error.Validation(
            "UnsupportedPaymentMethod",
            "Choose CreditCard, PayAtHotel, or MobileWallet.");

    public static Error InvalidRoomCount()
        => Error.Validation("InvalidRoomCount", "At least one room is required.");

    public static Error InvalidPriceRange()
        => Error.Validation("InvalidPriceRange", "Minimum price cannot be greater than maximum price.");

    public static Error InvalidPagination()
        => Error.Validation("InvalidPagination", "Page and page size must be greater than zero.");

    public static Error UnsupportedDestination(string destination)
        => Error.Business("UnsupportedDestination", $"No properties match the destination '{destination}'.");

    public static Error PropertyNotFound(Guid propertyId)
        => Error.NotFound("PropertyNotFound", $"Property '{propertyId}' was not found.");

    public static Error PropertyInactive(Guid propertyId)
        => Error.Business("PropertyInactive", $"Property '{propertyId}' is not available.");

    public static Error RoomNotFound(Guid roomTypeId)
        => Error.NotFound("RoomNotFound", $"Room type '{roomTypeId}' was not found.");

    public static Error RoomUnavailable()
        => Error.Business("RoomUnavailable", "The requested room is not available for the selected dates.");

    public static Error InvalidOccupancy()
        => Error.Validation("InvalidOccupancy", "Guest count exceeds the room occupancy limits.");

    public static Error BookingNotFound(Guid bookingId)
        => Error.NotFound("BookingNotFound", $"Booking '{bookingId}' was not found.");

    public static Error BookingNumberNotFound(string bookingNumber)
        => Error.NotFound("BookingNotFound", $"Booking '{bookingNumber}' was not found.");

    public static Error UnauthorizedBookingAccess()
        => Error.Authorization("UnauthorizedBookingAccess", "You are not allowed to access this booking.");

    public static Error BookingNotCancellable()
        => Error.Business("BookingNotCancellable", "This booking cannot be cancelled.");

    public static Error BookingNotModifiable()
        => Error.Business("BookingNotModifiable", "This booking cannot be modified.");

    public static Error BookingNotConfirmable()
        => Error.Business("BookingNotConfirmable", "Only pending bookings can be confirmed.");

    public static Error BookingNotCompletable()
        => Error.Business("BookingNotCompletable", "Only confirmed bookings can be completed.");

    public static Error BookingNotPayable()
        => Error.Business("BookingNotPayable", "This booking is not payable.");

    public static Error AuthenticationRequired()
        => Error.Authorization("AuthenticationRequired", "Authentication is required for this operation.");

    public static Error ReviewNotAllowed()
        => Error.Business("ReviewNotAllowed", "This booking is not eligible for review.");

    public static Error ReviewAlreadyExists()
        => Error.Conflict("ReviewAlreadyExists", "A review already exists for this booking.");

    public static Error InvalidRating()
        => Error.Validation("InvalidRating", "Rating must be between 1 and 5.");

    public static Error InsufficientLoyaltyPoints()
        => Error.Business("InsufficientLoyaltyPoints", "Insufficient loyalty points for this redemption.");

    public static Error InvalidPayment()
        => Error.Validation("InvalidPayment", "The payment request is invalid.");

    public static Error PaymentAlreadyProcessed()
        => Error.Conflict("PaymentAlreadyProcessed", "A successful payment already exists for this booking.");

    public static Error UnauthorizedPaymentAccess()
        => Error.Authorization("UnauthorizedPaymentAccess", "You are not allowed to access this payment.");

    public static Error UpgradeNotFound(Guid upgradeId)
        => Error.NotFound("UpgradeNotFound", $"Upgrade option '{upgradeId}' was not found.");

    public static Error UpgradeNotRemovable()
        => Error.Business("UpgradeNotRemovable", "Mandatory upgrades cannot be removed.");

    public static Error WishlistItemExists()
        => Error.Conflict("WishlistItemExists", "This property is already in the wishlist.");

    public static Error WishlistItemNotFound()
        => Error.NotFound("WishlistItemNotFound", "Wishlist item was not found.");

    public static Error InvalidLocation()
        => Error.Validation("InvalidLocation", "Latitude, longitude, and radius must be valid.");

    public static Error InvalidCredentials()
        => Error.Authorization("InvalidCredentials", "Invalid email or password.");

    public static Error EmailAlreadyRegistered()
        => Error.Conflict("EmailAlreadyRegistered", "An account with this email already exists.");

    public static Error UserNotFound()
        => Error.NotFound("UserNotFound", "User was not found.");

    public static Error InvalidEmail()
        => Error.Validation("InvalidEmail", "A valid email address is required.");

    public static Error WeakPassword()
        => Error.Validation("WeakPassword", "Password must be at least 8 characters.");

    public static Error InvalidRefreshToken()
        => Error.Authorization("InvalidRefreshToken", "The refresh token is invalid.");

    public static Error RevokedRefreshToken()
        => Error.Authorization("RevokedRefreshToken", "The refresh token has been revoked.");

    public static Error ExpiredRefreshToken()
        => Error.Authorization("ExpiredRefreshToken", "The refresh token has expired.");

    public static Error InvalidGoogleToken()
        => Error.Authorization("InvalidGoogleToken", "The Google credential is invalid.");

    public static Error ExpiredGoogleToken()
        => Error.Authorization("ExpiredGoogleToken", "The Google credential has expired.");

    public static Error InvalidAppleToken()
        => Error.Authorization("InvalidAppleToken", "The Apple credential is invalid.");

    public static Error ExpiredAppleToken()
        => Error.Authorization("ExpiredAppleToken", "The Apple credential has expired.");

    public static Error InvalidIssuer()
        => Error.Authorization("InvalidIssuer", "The identity token issuer is invalid.");

    public static Error InvalidAudience()
        => Error.Authorization("InvalidAudience", "The identity token audience is invalid.");

    public static Error ProviderUnavailable()
        => Error.Business("ProviderUnavailable", "The identity provider is unavailable or is not configured.");

    public static Error AccountDisabled()
        => Error.Authorization("AccountDisabled", "This account is disabled.");

    public static Error AccountConflict()
        => Error.Conflict(
            "AccountConflict",
            "An account with this email already exists. Sign in with your existing credentials.");

    public static Error RegistrationRequired()
        => Error.Business(
            "RegistrationRequired",
            "Additional registration information is required to complete sign-in.");

    public static Error AmenityNotFound(Guid amenityId)
        => Error.NotFound("AmenityNotFound", $"Amenity '{amenityId}' was not found.");

    public static Error RoleNotFound()
        => Error.NotFound("RoleNotFound", "Role was not found.");

    public static Error InvalidInventory()
        => Error.Validation("InvalidInventory", "Available rooms must be between 0 and total rooms.");

    public static Error PaymentNotFound(Guid paymentId)
        => Error.NotFound("PaymentNotFound", $"Payment '{paymentId}' was not found.");

    public static Error PaymentNotRefundable()
        => Error.Business("PaymentNotRefundable", "This payment cannot be refunded.");

    public static Error PaymentNotConfirmable()
        => Error.Business(
            "PaymentNotConfirmable",
            "Only pending pay-at-hotel payments can be confirmed.");

    public static Error ReviewNotFound(Guid reviewId)
        => Error.NotFound("ReviewNotFound", $"Review '{reviewId}' was not found.");

    public static Error InvalidLoyaltyAdjustment()
        => Error.Validation("InvalidLoyaltyAdjustment", "Loyalty adjustment must change the balance by a non-zero amount and cannot go below zero.");

    public static Error ImageNotFound(Guid imageId)
        => Error.NotFound("ImageNotFound", $"Image '{imageId}' was not found.");

    public static Error InvalidImageFile()
        => Error.Validation("InvalidImage", "Upload a JPG, PNG, WEBP, or GIF image up to 5 MB.");

    public static Error Business(string code, string message)
        => Error.Business(code, message);

    public static Error Validation(string code, string message, IReadOnlyDictionary<string, string[]>? errors = null)
        => Error.Validation(code, message, errors);

    public static Error PointsWouldReduceBelowZero()
        => Error.Business("PointsWouldReduceBelowZero", "Loyalty points cannot reduce the payable amount below zero.");
}
