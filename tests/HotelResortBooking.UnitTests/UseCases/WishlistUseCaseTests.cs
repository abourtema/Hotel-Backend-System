using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.UseCases.Wishlist;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.UnitTests.Fakes;
using Xunit;

namespace HotelResortBooking.UnitTests.UseCases;

public sealed class WishlistUseCaseTests
{
    private readonly FakeAccessContext _access = new();
    private readonly FakeWishlistRepository _wishlist = new();
    private readonly FakePropertyRepository _properties = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly AddToWishlistUseCase _add;
    private readonly RemoveFromWishlistUseCase _remove;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _propertyId = Guid.NewGuid();

    public WishlistUseCaseTests()
    {
        _add = new AddToWishlistUseCase(_access, _wishlist, _properties, _uow);
        _remove = new RemoveFromWishlistUseCase(_access, _wishlist, _uow);

        _properties.Seed(new PropertyDetailsData
        {
            Id = _propertyId,
            Name = "Wishlist Hotel",
            Description = "Desc",
            PropertyType = PropertyType.Hotel,
            City = "Paris",
            Country = "France",
            AddressLine = "1 Rue",
            CheckInTime = new TimeOnly(15, 0),
            CheckOutTime = new TimeOnly(11, 0)
        });
        _wishlist.PropertyNames[_propertyId] = "Wishlist Hotel";
    }

    [Fact]
    public async Task AddProperty_Succeeds()
    {
        _access.Authenticate(_userId);

        var result = await _add.ExecuteAsync(new AddToWishlistRequest { PropertyId = _propertyId });

        Assert.True(result.IsSuccess);
        Assert.Equal(_propertyId, result.Value.Item.PropertyId);
    }

    [Fact]
    public async Task DuplicateProperty_Fails()
    {
        _access.Authenticate(_userId);
        await _add.ExecuteAsync(new AddToWishlistRequest { PropertyId = _propertyId });

        var result = await _add.ExecuteAsync(new AddToWishlistRequest { PropertyId = _propertyId });

        Assert.True(result.IsFailure);
        Assert.Equal("WishlistItemExists", result.Error!.Code);
    }

    [Fact]
    public async Task RemoveProperty_Succeeds()
    {
        _access.Authenticate(_userId);
        await _add.ExecuteAsync(new AddToWishlistRequest { PropertyId = _propertyId });

        var result = await _remove.ExecuteAsync(new RemoveFromWishlistRequest { PropertyId = _propertyId });

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Removed);
    }

    [Fact]
    public async Task Unauthorized_NotAuthenticated_Fails()
    {
        _access.AsGuest();

        var result = await _add.ExecuteAsync(new AddToWishlistRequest { PropertyId = _propertyId });

        Assert.True(result.IsFailure);
        Assert.Equal("AuthenticationRequired", result.Error!.Code);
    }
}
