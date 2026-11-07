using HotelResortBooking.Application.Abstractions.Services;

namespace HotelResortBooking.Application.UseCases.Bookings;

public sealed class PriceBreakdownLineDto
{
    public string Label { get; init; } = string.Empty;

    public decimal Amount { get; init; }

    public bool IsTotal { get; init; }
}

internal static class BookingPriceBreakdown
{
    public static IReadOnlyList<PriceBreakdownLineDto> From(PricingResult pricing)
    {
        var nightLabel = pricing.Nights == 1 ? "1 night" : $"{pricing.Nights} nights";
        var lines = new List<PriceBreakdownLineDto>
        {
            new()
            {
                Label = $"Room ({nightLabel})",
                Amount = pricing.SubTotal
            }
        };

        if (pricing.UpgradeTotal > 0)
        {
            lines.Add(new PriceBreakdownLineDto
            {
                Label = "Upgrades",
                Amount = pricing.UpgradeTotal
            });
        }

        var discount = pricing.DiscountAmount + pricing.PointsDiscount;
        if (discount > 0)
        {
            lines.Add(new PriceBreakdownLineDto
            {
                Label = "Discount",
                Amount = -discount
            });
        }

        lines.Add(new PriceBreakdownLineDto
        {
            Label = "Tax",
            Amount = pricing.TaxAmount
        });

        lines.Add(new PriceBreakdownLineDto
        {
            Label = "Total",
            Amount = pricing.TotalAmount,
            IsTotal = true
        });

        return lines;
    }
}
