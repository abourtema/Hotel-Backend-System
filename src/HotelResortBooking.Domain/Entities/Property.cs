namespace HotelResortBooking.Domain.Entities;

using HotelResortBooking.Domain.Common;
using HotelResortBooking.Domain.Enums;

public sealed class Property : AggregateRoot
{
    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public PropertyType PropertyType { get; set; }

    public int StarRating { get; set; }

    public decimal ReviewScore { get; set; }

    public int ReviewCount { get; set; }

    public string AddressLine { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public string? PostalCode { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public TimeOnly CheckInTime { get; set; }

    public TimeOnly CheckOutTime { get; set; }

    public string? CancellationPolicy { get; set; }

    public string? DepositPolicy { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<PropertyImage> Images { get; set; } = new List<PropertyImage>();

    public ICollection<PropertyAmenity> PropertyAmenities { get; set; } = new List<PropertyAmenity>();

    public ICollection<RoomType> RoomTypes { get; set; } = new List<RoomType>();

    public ICollection<UpgradeOption> UpgradeOptions { get; set; } = new List<UpgradeOption>();

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public ICollection<Review> Reviews { get; set; } = new List<Review>();

    public ICollection<WishlistItem> WishlistItems { get; set; } = new List<WishlistItem>();
}
