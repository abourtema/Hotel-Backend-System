using HotelResortBooking.Application.Abstractions.Services;
using HotelResortBooking.Application.Services;
using Xunit;

namespace HotelResortBooking.UnitTests.Services;

public sealed class PricingServiceTests
{
    private readonly PricingService _sut = new();

    [Fact]
    public void Calculate_ComputesNightsSubtotalUpgradesTaxPointsDiscountAndTotal()
    {
        var result = _sut.Calculate(new PricingRequest
        {
            RoomPricePerNight = 100m,
            Nights = 3,
            RoomQuantity = 2,
            Upgrades =
            [
                new UpgradePriceLine { UpgradeOptionId = Guid.NewGuid(), UnitPrice = 25m, Quantity = 2 }
            ],
            DiscountAmount = 10m,
            PointsToRedeem = 500,
            PointValue = 0.01m,
            TaxRate = 0.10m,
            Currency = "USD"
        });

        // SubTotal = 100 * 3 * 2 = 600
        // Upgrades = 25 * 2 = 50
        // Points discount = 500 * 0.01 = 5
        // Taxable = 600 + 50 - 10 - 5 = 635
        // Tax = 63.50
        // Total = 698.50
        Assert.Equal(3, result.Nights);
        Assert.Equal(600m, result.SubTotal);
        Assert.Equal(50m, result.UpgradeTotal);
        Assert.Equal(10m, result.DiscountAmount);
        Assert.Equal(5m, result.PointsDiscount);
        Assert.Equal(500, result.PointsRedeemed);
        Assert.Equal(63.50m, result.TaxAmount);
        Assert.Equal(698.50m, result.TotalAmount);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public void Calculate_PointsCannotReduceTotalBelowZero()
    {
        var result = _sut.Calculate(new PricingRequest
        {
            RoomPricePerNight = 50m,
            Nights = 1,
            RoomQuantity = 1,
            PointsToRedeem = 100_000,
            PointValue = 0.01m,
            TaxRate = 0.10m,
            Currency = "USD"
        });

        Assert.True(result.TotalAmount >= 0m);
        Assert.Equal(0m, result.TotalAmount);
        Assert.Equal(50m, result.PointsDiscount);
    }

    [Fact]
    public void CalculateNights_ReturnsPositiveDifference()
    {
        var nights = _sut.CalculateNights(new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 13));
        Assert.Equal(3, nights);
    }
}
