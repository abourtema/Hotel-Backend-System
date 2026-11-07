using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Services;
using HotelResortBooking.Application.UseCases.Rooms;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.UnitTests.Fakes;
using Xunit;

namespace HotelResortBooking.UnitTests.UseCases;

public sealed class GetAvailableRoomsUseCaseTests
{
    private readonly FakePropertyRepository _properties = new();
    private readonly FakeRoomTypeRepository _rooms = new();
    private readonly GetAvailableRoomsUseCase _sut;

    public GetAvailableRoomsUseCaseTests()
    {
        _sut = new GetAvailableRoomsUseCase(_properties, _rooms, new PricingService());
    }

    [Fact]
    public async Task AvailableRoom_ReturnedWithPricing()
    {
        var propertyId = Guid.NewGuid();
        var roomTypeId = Guid.NewGuid();
        SeedActiveProperty(propertyId);
        _rooms.Rooms.Add(new AvailableRoomData
        {
            RoomTypeId = roomTypeId,
            PropertyId = propertyId,
            Name = "Deluxe",
            Description = "Nice room",
            MaxAdults = 2,
            MaxChildren = 2,
            MaxGuests = 4,
            PricePerNight = 100m,
            Currency = "USD",
            AvailableCount = 3
        });

        var result = await _sut.ExecuteAsync(new GetAvailableRoomsRequest
        {
            PropertyId = propertyId,
            CheckInDate = new DateOnly(2026, 8, 20),
            CheckOutDate = new DateOnly(2026, 8, 22),
            Adults = 2,
            Rooms = 1
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Nights);
        Assert.Single(result.Value.Rooms);
        // 100 * 2 nights * 1 room = 200 + 10% tax = 220
        Assert.Equal(220m, result.Value.Rooms[0].TotalPrice);
        Assert.Equal(roomTypeId, result.Value.Rooms[0].RoomTypeId);
    }

    [Fact]
    public async Task UnavailableRoom_ReturnsEmptyList()
    {
        var propertyId = Guid.NewGuid();
        SeedActiveProperty(propertyId);
        _rooms.Rooms.Add(new AvailableRoomData
        {
            RoomTypeId = Guid.NewGuid(),
            PropertyId = propertyId,
            Name = "Sold Out",
            MaxAdults = 2,
            MaxChildren = 1,
            MaxGuests = 3,
            PricePerNight = 80m,
            Currency = "USD",
            AvailableCount = 0
        });

        var result = await _sut.ExecuteAsync(new GetAvailableRoomsRequest
        {
            PropertyId = propertyId,
            CheckInDate = new DateOnly(2026, 8, 20),
            CheckOutDate = new DateOnly(2026, 8, 22),
            Adults = 1,
            Rooms = 1
        });

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Rooms);
    }

    [Fact]
    public async Task InvalidOccupancy_ExcludesRooms()
    {
        var propertyId = Guid.NewGuid();
        SeedActiveProperty(propertyId);
        _rooms.Rooms.Add(new AvailableRoomData
        {
            RoomTypeId = Guid.NewGuid(),
            PropertyId = propertyId,
            Name = "Single",
            MaxAdults = 1,
            MaxChildren = 0,
            MaxGuests = 1,
            PricePerNight = 70m,
            Currency = "USD",
            AvailableCount = 5
        });

        var result = await _sut.ExecuteAsync(new GetAvailableRoomsRequest
        {
            PropertyId = propertyId,
            CheckInDate = new DateOnly(2026, 8, 20),
            CheckOutDate = new DateOnly(2026, 8, 22),
            Adults = 4,
            Rooms = 1
        });

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Rooms);
    }

    [Fact]
    public async Task InvalidDates_Fails()
    {
        var result = await _sut.ExecuteAsync(new GetAvailableRoomsRequest
        {
            PropertyId = Guid.NewGuid(),
            CheckInDate = new DateOnly(2026, 8, 22),
            CheckOutDate = new DateOnly(2026, 8, 20),
            Adults = 1,
            Rooms = 1
        });

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidDateRange", result.Error!.Code);
    }

    private void SeedActiveProperty(Guid propertyId)
    {
        _properties.Seed(new PropertyDetailsData
        {
            Id = propertyId,
            Name = "Hotel",
            Description = "Desc",
            PropertyType = PropertyType.Hotel,
            City = "City",
            Country = "Country",
            AddressLine = "Addr",
            CheckInTime = new TimeOnly(15, 0),
            CheckOutTime = new TimeOnly(11, 0)
        });
    }
}
