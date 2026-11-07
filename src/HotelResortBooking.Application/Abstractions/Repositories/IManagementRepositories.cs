using HotelResortBooking.Application.Common.Paging;
using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.Abstractions.Repositories;

public sealed class ManagementUserListItem
{
    public Guid Id { get; init; }

    public string Email { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string? PhoneNumber { get; init; }

    public bool IsActive { get; init; }

    public string PrimaryRole { get; init; } = string.Empty;

    public IReadOnlyList<ManagementRoleDto> Roles { get; init; } = Array.Empty<ManagementRoleDto>();

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class ManagementRoleDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
}

public sealed class CreateManagementUserModel
{
    public string Email { get; init; } = string.Empty;

    public string PasswordHash { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string? PhoneNumber { get; init; }

    public Guid? RoleId { get; init; }
}

public interface IRolePermissionRepository
{
    Task<IReadOnlyList<ManagementRoleDto>> GetRolesAsync(CancellationToken cancellationToken = default);

    Task<ManagementRoleDto?> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken = default);

    Task<ManagementRoleDto?> GetRoleByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetPermissionNamesForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetRoleNamesForUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

public interface IManagementUserRepository
{
    Task<PagedResult<ManagementUserListItem>> SearchAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ManagementUserListItem?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<ManagementUserListItem> CreateAsync(CreateManagementUserModel model, CancellationToken cancellationToken = default);

    Task UpdateAsync(
        Guid userId,
        string firstName,
        string lastName,
        string? phoneNumber,
        CancellationToken cancellationToken = default);

    Task SetActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default);

    Task AssignRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

    Task RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
}

public sealed class ManagedHotelListItem
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public PropertyType PropertyType { get; init; }

    public int StarRating { get; init; }

    public bool IsActive { get; init; }
}

public sealed class ManagedHotelDetails
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public PropertyType PropertyType { get; init; }

    public int StarRating { get; init; }

    public string AddressLine { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public TimeOnly CheckInTime { get; init; }

    public TimeOnly CheckOutTime { get; init; }

    public string? CancellationPolicy { get; init; }

    public string? DepositPolicy { get; init; }

    public bool IsActive { get; init; }

    public IReadOnlyList<ManagedImageDto> Images { get; init; } = Array.Empty<ManagedImageDto>();

    public IReadOnlyList<ManagedAmenityDto> Amenities { get; init; } = Array.Empty<ManagedAmenityDto>();
}

public sealed class ManagedImageDto
{
    public Guid Id { get; init; }

    public string ImageUrl { get; init; } = string.Empty;

    public string? AltText { get; init; }

    public int SortOrder { get; init; }

    public bool IsPrimary { get; init; }
}

public sealed class SaveHotelModel
{
    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public PropertyType PropertyType { get; init; }

    public int StarRating { get; init; }

    public string AddressLine { get; init; } = string.Empty;

    public string City { get; init; } = string.Empty;

    public string Country { get; init; } = string.Empty;

    public double Latitude { get; init; }

    public double Longitude { get; init; }

    public TimeOnly CheckInTime { get; init; }

    public TimeOnly CheckOutTime { get; init; }

    public string? CancellationPolicy { get; init; }

    public string? DepositPolicy { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class ManagedAmenityDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public string? Icon { get; init; }

    public bool IsActive { get; init; }
}

public sealed class ManagedRoomDto
{
    public Guid Id { get; init; }

    public Guid PropertyId { get; init; }

    public string PropertyName { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public decimal? RoomSize { get; init; }

    public int MaxAdults { get; init; }

    public int MaxChildren { get; init; }

    public int MaxGuests { get; init; }

    public string? BedType { get; init; }

    public decimal BasePricePerNight { get; init; }

    public string Currency { get; init; } = string.Empty;

    public bool IsActive { get; init; }

    public IReadOnlyList<ManagedImageDto> Images { get; init; } = Array.Empty<ManagedImageDto>();
}

public sealed class SaveRoomTypeModel
{
    public Guid PropertyId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public decimal? RoomSize { get; init; }

    public int MaxAdults { get; init; }

    public int MaxChildren { get; init; }

    public int MaxGuests { get; init; }

    public string? BedType { get; init; }

    public decimal BasePricePerNight { get; init; }

    public string Currency { get; init; } = string.Empty;

    public bool IsActive { get; init; } = true;
}

public sealed class RoomInventoryDto
{
    public Guid Id { get; init; }

    public DateOnly Date { get; init; }

    public int TotalRooms { get; init; }

    public int AvailableRooms { get; init; }
}

public sealed class RoomInventoryUpdateItem
{
    public DateOnly Date { get; init; }

    public int TotalRooms { get; init; }

    public int AvailableRooms { get; init; }
}

public sealed class ManagedUpgradeDto
{
    public Guid Id { get; init; }

    public Guid PropertyId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public UpgradeType UpgradeType { get; init; }

    public decimal Price { get; init; }

    public string Currency { get; init; } = string.Empty;

    public bool IsMandatory { get; init; }

    public bool IsRecommended { get; init; }

    public bool IsActive { get; init; }

    public IReadOnlyList<Guid> RoomTypeIds { get; init; } = Array.Empty<Guid>();
}

public sealed class SaveUpgradeModel
{
    public Guid PropertyId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Description { get; init; }

    public UpgradeType UpgradeType { get; init; }

    public decimal Price { get; init; }

    public string Currency { get; init; } = string.Empty;

    public bool IsMandatory { get; init; }

    public bool IsRecommended { get; init; }

    public bool IsActive { get; init; } = true;

    public IReadOnlyList<Guid> RoomTypeIds { get; init; } = Array.Empty<Guid>();
}

public interface IManagementCatalogRepository
{
    Task<PagedResult<ManagedHotelListItem>> SearchHotelsAsync(
        string? search,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ManagedHotelDetails?> GetHotelAsync(Guid hotelId, CancellationToken cancellationToken = default);

    Task<ManagedHotelDetails> CreateHotelAsync(SaveHotelModel model, CancellationToken cancellationToken = default);

    Task<ManagedHotelDetails?> UpdateHotelAsync(Guid hotelId, SaveHotelModel model, CancellationToken cancellationToken = default);

    Task<bool> SetHotelActiveAsync(Guid hotelId, bool isActive, CancellationToken cancellationToken = default);

    Task<ManagedImageDto> AddHotelImageAsync(
        Guid hotelId,
        string imageUrl,
        string? altText,
        int? sortOrder,
        bool isPrimary,
        CancellationToken cancellationToken = default);

    Task<ManagedImageDto?> UpdateHotelImageAsync(
        Guid hotelId,
        Guid imageId,
        string? imageUrl,
        string? altText,
        int? sortOrder,
        bool? isPrimary,
        CancellationToken cancellationToken = default);

    Task<string?> DeleteHotelImageAsync(Guid hotelId, Guid imageId, CancellationToken cancellationToken = default);

    Task<bool> SetPrimaryHotelImageAsync(Guid hotelId, Guid imageId, CancellationToken cancellationToken = default);

    Task ReorderHotelImagesAsync(Guid hotelId, IReadOnlyList<Guid> imageIdsInOrder, CancellationToken cancellationToken = default);

    Task<ManagedImageDto> AddRoomImageAsync(
        Guid roomTypeId,
        string imageUrl,
        string? altText,
        int? sortOrder,
        bool isPrimary,
        CancellationToken cancellationToken = default);

    Task<ManagedImageDto?> UpdateRoomImageAsync(
        Guid roomTypeId,
        Guid imageId,
        string? imageUrl,
        string? altText,
        int? sortOrder,
        bool? isPrimary,
        CancellationToken cancellationToken = default);

    Task<string?> DeleteRoomImageAsync(Guid roomTypeId, Guid imageId, CancellationToken cancellationToken = default);

    Task<bool> SetPrimaryRoomImageAsync(Guid roomTypeId, Guid imageId, CancellationToken cancellationToken = default);

    Task ReorderRoomImagesAsync(Guid roomTypeId, IReadOnlyList<Guid> imageIdsInOrder, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ManagedAmenityDto>> GetAmenitiesAsync(CancellationToken cancellationToken = default);

    Task<ManagedAmenityDto> CreateAmenityAsync(string name, string? description, string? icon, CancellationToken cancellationToken = default);

    Task<ManagedAmenityDto?> UpdateAmenityAsync(Guid id, string name, string? description, string? icon, CancellationToken cancellationToken = default);

    Task<bool> SetAmenityActiveAsync(Guid id, bool isActive, CancellationToken cancellationToken = default);

    Task AssignAmenityToPropertyAsync(Guid propertyId, Guid amenityId, CancellationToken cancellationToken = default);

    Task RemoveAmenityFromPropertyAsync(Guid propertyId, Guid amenityId, CancellationToken cancellationToken = default);

    Task<PagedResult<ManagedRoomDto>> SearchRoomsAsync(
        Guid? propertyId,
        bool? isActive,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ManagedRoomDto?> GetRoomAsync(Guid roomTypeId, CancellationToken cancellationToken = default);

    Task<ManagedRoomDto> CreateRoomAsync(SaveRoomTypeModel model, CancellationToken cancellationToken = default);

    Task<ManagedRoomDto?> UpdateRoomAsync(Guid roomTypeId, SaveRoomTypeModel model, CancellationToken cancellationToken = default);

    Task<bool> SetRoomActiveAsync(Guid roomTypeId, bool isActive, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoomInventoryDto>> GetInventoryAsync(
        Guid roomTypeId,
        DateOnly from,
        DateOnly to,
        CancellationToken cancellationToken = default);

    Task UpsertInventoryAsync(
        Guid roomTypeId,
        IReadOnlyList<RoomInventoryUpdateItem> items,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ManagedUpgradeDto>> GetUpgradesAsync(Guid? propertyId, CancellationToken cancellationToken = default);

    Task<ManagedUpgradeDto?> GetUpgradeAsync(Guid upgradeId, CancellationToken cancellationToken = default);

    Task<ManagedUpgradeDto> CreateUpgradeAsync(SaveUpgradeModel model, CancellationToken cancellationToken = default);

    Task<ManagedUpgradeDto?> UpdateUpgradeAsync(Guid upgradeId, SaveUpgradeModel model, CancellationToken cancellationToken = default);

    Task<bool> DeleteUpgradeAsync(Guid upgradeId, CancellationToken cancellationToken = default);
}

public sealed class ManagedBookingListItem
{
    public Guid Id { get; init; }

    public string BookingNumber { get; init; } = string.Empty;

    public Guid? UserId { get; init; }

    public Guid PropertyId { get; init; }

    public string PropertyName { get; init; } = string.Empty;

    public DateOnly CheckInDate { get; init; }

    public DateOnly CheckOutDate { get; init; }

    public BookingStatus Status { get; init; }

    public int Adults { get; init; }

    public int Children { get; init; }

    public int RoomsCount { get; init; }

    public string? SpecialRequests { get; init; }

    public string? GuestFirstName { get; init; }

    public string? GuestLastName { get; init; }

    public string? GuestEmail { get; init; }

    public string? GuestPhoneNumber { get; init; }

    public PaymentStatus? PaymentStatus { get; init; }

    public PaymentMethodType? PaymentMethod { get; init; }

    public decimal TotalAmount { get; init; }

    public string Currency { get; init; } = string.Empty;

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class ManagedCustomerListItem
{
    public Guid Id { get; init; }

    public string Email { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string? PhoneNumber { get; init; }

    public bool IsActive { get; init; }

    public int BookingCount { get; init; }

    public int CompletedBookingCount { get; init; }

    public int LoyaltyBalance { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public string? City { get; init; }

    public string? Country { get; init; }
}

public sealed class ManagedPaymentListItem
{
    public Guid Id { get; init; }

    public Guid BookingId { get; init; }

    public Guid? UserId { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; } = string.Empty;

    public PaymentStatus Status { get; init; }

    public PaymentMethodType PaymentMethod { get; init; }

    public string? CardBrand { get; init; }

    public string? CardLast4 { get; init; }

    public string? ProviderTransactionId { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? PaidAt { get; init; }
}

public sealed class ManagedReviewListItem
{
    public Guid Id { get; init; }

    public Guid UserId { get; init; }

    public Guid PropertyId { get; init; }

    public Guid BookingId { get; init; }

    public int Rating { get; init; }

    public string? Comment { get; init; }

    public ReviewStatus Status { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class ManagementDashboardTotals
{
    public int TotalHotels { get; init; }

    public int ActiveHotels { get; init; }

    public int TotalRooms { get; init; }

    public int AvailableRooms { get; init; }

    public int TotalBookings { get; init; }

    public int PendingBookings { get; init; }

    public int ConfirmedBookings { get; init; }

    public int CancelledBookings { get; init; }

    public int CompletedBookings { get; init; }

    public int TotalCustomers { get; init; }

    public decimal TotalRevenue { get; init; }

    public int TotalPayments { get; init; }

    public int SuccessfulPayments { get; init; }

    public int FailedPayments { get; init; }

    public int CurrentLoyaltyPoints { get; init; }

    public int ReviewCount { get; init; }
}

public sealed class BookingsReportRow
{
    public BookingStatus Status { get; init; }

    public int Count { get; init; }

    public decimal TotalAmount { get; init; }
}

public sealed class RevenueReportRow
{
    public DateOnly Date { get; init; }

    public decimal Revenue { get; init; }

    public int BookingCount { get; init; }
}

public sealed class OccupancyReportRow
{
    public DateOnly Date { get; init; }

    public int TotalRooms { get; init; }

    public int OccupiedRooms { get; init; }

    public decimal OccupancyRate { get; init; }
}

public sealed class PaymentsReportRow
{
    public PaymentStatus Status { get; init; }

    public int Count { get; init; }

    public decimal Amount { get; init; }
}

public interface IManagementQueryRepository
{
    Task<ManagementDashboardTotals> GetDashboardAsync(
        DateTimeOffset? dateFrom,
        DateTimeOffset? dateTo,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ManagedBookingListItem>> SearchBookingsAsync(
        string? bookingNumber,
        Guid? userId,
        Guid? propertyId,
        BookingStatus? status,
        DateOnly? checkInDate,
        DateOnly? checkOutDate,
        PaymentStatus? paymentStatus,
        PaymentMethodType? paymentMethod,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ManagedCustomerListItem>> SearchCustomersAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ManagedCustomerListItem?> GetCustomerAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<PagedResult<ManagedPaymentListItem>> SearchPaymentsAsync(
        Guid? bookingId,
        Guid? userId,
        PaymentStatus? status,
        PaymentMethodType? paymentMethod,
        DateTimeOffset? dateFrom,
        DateTimeOffset? dateTo,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ManagedReviewListItem>> SearchReviewsAsync(
        ReviewStatus? status,
        Guid? propertyId,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ManagedReviewListItem?> GetReviewAsync(Guid reviewId, CancellationToken cancellationToken = default);

    Task<bool> SetReviewStatusAsync(Guid reviewId, ReviewStatus status, CancellationToken cancellationToken = default);

    Task<bool> SetCustomerActiveAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<BookingsReportRow>> GetBookingsReportAsync(
        DateTimeOffset? dateFrom,
        DateTimeOffset? dateTo,
        Guid? propertyId,
        BookingStatus? status,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RevenueReportRow>> GetRevenueReportAsync(
        DateTimeOffset? dateFrom,
        DateTimeOffset? dateTo,
        Guid? propertyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OccupancyReportRow>> GetOccupancyReportAsync(
        DateOnly from,
        DateOnly to,
        Guid? propertyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PaymentsReportRow>> GetPaymentsReportAsync(
        DateTimeOffset? dateFrom,
        DateTimeOffset? dateTo,
        Guid? propertyId,
        PaymentStatus? status,
        CancellationToken cancellationToken = default);
}
