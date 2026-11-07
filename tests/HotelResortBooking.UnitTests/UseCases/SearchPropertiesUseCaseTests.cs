using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.UseCases.Search;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.UnitTests.Fakes;
using Xunit;

namespace HotelResortBooking.UnitTests.UseCases;

public sealed class SearchPropertiesUseCaseTests
{
    private readonly FakePropertyRepository _properties = new();
    private readonly SearchPropertiesUseCase _sut;

    public SearchPropertiesUseCaseTests()
    {
        _sut = new SearchPropertiesUseCase(_properties);
    }

    [Fact]
    public async Task ValidGuestSearch_Succeeds()
    {
        var propertyId = Guid.NewGuid();
        _properties.Seed(CreateDetails(propertyId, "Paris Hotel", "Paris", "France"));

        var result = await _sut.ExecuteAsync(ValidRequest("Paris"));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.TotalCount);
        Assert.Single(result.Value.Items);
        Assert.Equal(propertyId, result.Value.Items[0].PropertyId);
    }

    [Fact]
    public async Task InvalidDateRange_Fails()
    {
        var result = await _sut.ExecuteAsync(new SearchPropertiesRequest
        {
            Destination = "Paris",
            CheckInDate = new DateOnly(2026, 8, 15),
            CheckOutDate = new DateOnly(2026, 8, 10),
            Adults = 2,
            Rooms = 1,
            Page = 1,
            PageSize = 20
        });

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidDateRange", result.Error!.Code);
    }

    [Fact]
    public async Task InvalidGuestCount_Fails()
    {
        var result = await _sut.ExecuteAsync(new SearchPropertiesRequest
        {
            Destination = "Paris",
            CheckInDate = new DateOnly(2026, 8, 20),
            CheckOutDate = new DateOnly(2026, 8, 23),
            Adults = 0,
            Rooms = 1,
            Page = 1,
            PageSize = 20
        });

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidGuestCount", result.Error!.Code);
    }

    [Fact]
    public async Task InvalidRoomCount_Fails()
    {
        var result = await _sut.ExecuteAsync(new SearchPropertiesRequest
        {
            Destination = "Paris",
            CheckInDate = new DateOnly(2026, 8, 20),
            CheckOutDate = new DateOnly(2026, 8, 23),
            Adults = 2,
            Rooms = 0,
            Page = 1,
            PageSize = 20
        });

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidRoomCount", result.Error!.Code);
    }

    [Fact]
    public async Task UnsupportedDestination_Fails()
    {
        _properties.Seed(CreateDetails(Guid.NewGuid(), "Paris Hotel", "Paris", "France"));

        var result = await _sut.ExecuteAsync(ValidRequest("Atlantis"));

        Assert.True(result.IsFailure);
        Assert.Equal("UnsupportedDestination", result.Error!.Code);
    }

    [Fact]
    public async Task EmptyResults_WhenDestinationExistsButNoMatches_SucceedsWithZeroItems()
    {
        _properties.KnownDestinations.Add("Paris");
        _properties.Seed(CreateDetails(Guid.NewGuid(), "Rome Inn", "Rome", "Italy"));

        var result = await _sut.ExecuteAsync(ValidRequest("Paris"));

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.TotalCount);
        Assert.Empty(result.Value.Items);
    }

    private static SearchPropertiesRequest ValidRequest(string destination) => new()
    {
        Destination = destination,
        CheckInDate = new DateOnly(2026, 8, 20),
        CheckOutDate = new DateOnly(2026, 8, 23),
        Adults = 2,
        Children = 0,
        Rooms = 1,
        Page = 1,
        PageSize = 20
    };

    private static PropertyDetailsData CreateDetails(Guid id, string name, string city, string country) => new()
    {
        Id = id,
        Name = name,
        Description = "Desc",
        PropertyType = PropertyType.Hotel,
        StarRating = 4,
        City = city,
        Country = country,
        AddressLine = "1 Main",
        CheckInTime = new TimeOnly(15, 0),
        CheckOutTime = new TimeOnly(11, 0),
        Rooms =
        [
            new RoomTypeSummaryData
            {
                Id = Guid.NewGuid(),
                Name = "Standard",
                MaxAdults = 2,
                MaxChildren = 1,
                MaxGuests = 3,
                BasePricePerNight = 120m,
                Currency = "USD"
            }
        ]
    };
}
