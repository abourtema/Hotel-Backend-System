namespace HotelResortBooking.Domain.Entities;

public sealed class PropertyAmenity
{
    public Guid PropertyId { get; set; }

    public Guid AmenityId { get; set; }

    public Property Property { get; set; } = null!;

    public Amenity Amenity { get; set; } = null!;
}
