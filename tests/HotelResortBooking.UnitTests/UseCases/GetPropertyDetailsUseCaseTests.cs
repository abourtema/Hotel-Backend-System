using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.UseCases.Properties;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.UnitTests.Fakes;
using Xunit;

namespace HotelResortBooking.UnitTests.UseCases;

public sealed class GetPropertyDetailsUseCaseTests
{
    private readonly FakePropertyRepository _properties = new();
    private readonly GetPropertyDetailsUseCase _sut;

    public GetPropertyDetailsUseCaseTests()
    {
        _sut = new GetPropertyDetailsUseCase(_properties);
    }

    [Fact]
    public async Task PropertyFound_Succeeds()
    {
        var id = Guid.NewGuid();
        _properties.Seed(CreateDetails(id, "Seaside Resort"));

        var result = await _sut.ExecuteAsync(new GetPropertyDetailsRequest { PropertyId = id });

        Assert.True(result.IsSuccess);
        Assert.Equal(id, result.Value.Id);
        Assert.Equal("Seaside Resort", result.Value.Name);
    }

    [Fact]
    public async Task PropertyNotFound_Fails()
    {
        var result = await _sut.ExecuteAsync(new GetPropertyDetailsRequest { PropertyId = Guid.NewGuid() });

        Assert.True(result.IsFailure);
        Assert.Equal("PropertyNotFound", result.Error!.Code);
    }

    [Fact]
    public async Task InactiveProperty_FailsWithPropertyInactive()
    {
        var id = Guid.NewGuid();
        _properties.Seed(CreateDetails(id, "Closed Hotel"), isActive: false);

        var result = await _sut.ExecuteAsync(new GetPropertyDetailsRequest { PropertyId = id });

        Assert.True(result.IsFailure);
        Assert.Equal("PropertyInactive", result.Error!.Code);
    }

    private static PropertyDetailsData CreateDetails(Guid id, string name) => new()
    {
        Id = id,
        Name = name,
        Description = "Desc",
        PropertyType = PropertyType.Resort,
        StarRating = 5,
        City = "Nice",
        Country = "France",
        AddressLine = "1 Beach",
        CheckInTime = new TimeOnly(15, 0),
        CheckOutTime = new TimeOnly(11, 0)
    };
}
