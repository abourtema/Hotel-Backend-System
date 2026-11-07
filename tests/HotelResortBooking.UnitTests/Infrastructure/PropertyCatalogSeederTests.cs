using HotelResortBooking.Domain.Enums;
using HotelResortBooking.Infrastructure.Identity;
using Xunit;

namespace HotelResortBooking.UnitTests.Infrastructure;

public sealed class PropertyCatalogSeederTests
{
    [Fact]
    public void Catalog_HasExactlyTwoPropertiesPerType()
    {
        var definitions = PropertyCatalogSeeder.Definitions;

        Assert.Equal(10, definitions.Count);
        Assert.Equal(10, definitions.Select(d => d.Id).Distinct().Count());
        Assert.Equal(2, definitions.Count(d => d.PropertyType == PropertyType.Hotel));
        Assert.Equal(2, definitions.Count(d => d.PropertyType == PropertyType.Resort));
        Assert.Equal(2, definitions.Count(d => d.PropertyType == PropertyType.Apartment));
        Assert.Equal(2, definitions.Count(d => d.PropertyType == PropertyType.Villa));
        Assert.Equal(2, definitions.Count(d => d.PropertyType == PropertyType.GuestHouse));
    }

    [Fact]
    public void Catalog_EachPropertyHasRoomsImagesAndLocation()
    {
        foreach (var property in PropertyCatalogSeeder.Definitions)
        {
            Assert.False(string.IsNullOrWhiteSpace(property.Name));
            Assert.False(string.IsNullOrWhiteSpace(property.City));
            Assert.InRange(property.Latitude, -90, 90);
            Assert.InRange(property.Longitude, -180, 180);
            Assert.NotEmpty(property.Images);
            Assert.NotEmpty(property.Rooms);
            Assert.NotEmpty(property.AmenityNames);
            Assert.All(property.Images, image =>
            {
                Assert.False(string.IsNullOrWhiteSpace(image.SourceUrl));
                Assert.StartsWith("https://images.unsplash.com/", image.SourceUrl);
            });
            Assert.All(property.Rooms, room =>
            {
                Assert.True(room.MaxGuests > 0);
                Assert.True(room.PricePerNight > 0);
                Assert.True(room.Inventory > 0);
            });
        }
    }

    [Fact]
    public void Catalog_PreservesRadissonBluId()
    {
        Assert.Contains(
            PropertyCatalogSeeder.Definitions,
            d => d.Id == PropertyCatalogSeeder.RadissonBluPropertyId
                 && d.PropertyType == PropertyType.Hotel);
    }
}
