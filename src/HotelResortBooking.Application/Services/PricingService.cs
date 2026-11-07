using HotelResortBooking.Application.Abstractions.Services;

namespace HotelResortBooking.Application.Services;

public sealed class PricingService : IPricingService
{
    public int CalculateNights(DateOnly checkIn, DateOnly checkOut)
    {
        var nights = checkOut.DayNumber - checkIn.DayNumber;
        return nights > 0 ? nights : 0;
    }

    public decimal CalculatePointsValue(int points, decimal pointValue = 0.01m)
        => Math.Max(0, points) * pointValue;

    public PricingResult Calculate(PricingRequest request)
    {
        var nights = Math.Max(0, request.Nights);
        var roomQuantity = Math.Max(0, request.RoomQuantity);
        var subTotal = decimal.Round(request.RoomPricePerNight * nights * roomQuantity, 2, MidpointRounding.AwayFromZero);

        var upgradeTotal = decimal.Round(
            request.Upgrades.Sum(u => u.UnitPrice * Math.Max(0, u.Quantity)),
            2,
            MidpointRounding.AwayFromZero);

        var discount = Math.Max(0, request.DiscountAmount);
        var pointsDiscount = CalculatePointsValue(request.PointsToRedeem, request.PointValue);

        var taxableBase = Math.Max(0, subTotal + upgradeTotal - discount - pointsDiscount);
        if (pointsDiscount > subTotal + upgradeTotal - discount)
        {
            pointsDiscount = Math.Max(0, subTotal + upgradeTotal - discount);
            taxableBase = 0;
        }

        var taxAmount = decimal.Round(taxableBase * Math.Max(0, request.TaxRate), 2, MidpointRounding.AwayFromZero);
        var total = decimal.Round(taxableBase + taxAmount, 2, MidpointRounding.AwayFromZero);

        return new PricingResult
        {
            Nights = nights,
            SubTotal = subTotal,
            UpgradeTotal = upgradeTotal,
            DiscountAmount = discount,
            TaxAmount = taxAmount,
            PointsRedeemed = request.PointsToRedeem,
            PointsDiscount = pointsDiscount,
            TotalAmount = total,
            Currency = request.Currency
        };
    }
}
