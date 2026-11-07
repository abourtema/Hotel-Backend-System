namespace HotelResortBooking.Application.Abstractions.Services;

public sealed class PricingRequest
{
    public decimal RoomPricePerNight { get; init; }

    public int Nights { get; init; }

    public int RoomQuantity { get; init; }

    public IReadOnlyList<UpgradePriceLine> Upgrades { get; init; } = Array.Empty<UpgradePriceLine>();

    public decimal DiscountAmount { get; init; }

    public int PointsToRedeem { get; init; }

    public decimal PointValue { get; init; } = 0.01m;

    public decimal TaxRate { get; init; } = 0.10m;

    public string Currency { get; init; } = string.Empty;
}

public sealed class UpgradePriceLine
{
    public Guid UpgradeOptionId { get; init; }

    public decimal UnitPrice { get; init; }

    public int Quantity { get; init; }
}

public sealed class PricingResult
{
    public int Nights { get; init; }

    public decimal SubTotal { get; init; }

    public decimal UpgradeTotal { get; init; }

    public decimal DiscountAmount { get; init; }

    public decimal TaxAmount { get; init; }

    public int PointsRedeemed { get; init; }

    public decimal PointsDiscount { get; init; }

    public decimal TotalAmount { get; init; }

    public string Currency { get; init; } = string.Empty;
}

public interface IPricingService
{
    int CalculateNights(DateOnly checkIn, DateOnly checkOut);

    PricingResult Calculate(PricingRequest request);

    decimal CalculatePointsValue(int points, decimal pointValue = 0.01m);
}

public sealed class AvailabilityCheckRequest
{
    public Guid RoomTypeId { get; init; }

    public DateOnly CheckInDate { get; init; }

    public DateOnly CheckOutDate { get; init; }

    public int RoomsRequested { get; init; }
}

public sealed class AvailabilityCheckResult
{
    public bool IsAvailable { get; init; }

    public int AvailableRooms { get; init; }
}

public interface IRoomAvailabilityService
{
    Task<AvailabilityCheckResult> CheckAsync(
        AvailabilityCheckRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> ReserveAsync(
        AvailabilityCheckRequest request,
        CancellationToken cancellationToken = default);

    Task ReleaseAsync(
        AvailabilityCheckRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class PaymentGatewayChargeRequest
{
    public string? PaymentMethodToken { get; init; }

    public string? SavedProviderPaymentMethodId { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; } = string.Empty;

    public string IdempotencyKey { get; init; } = string.Empty;

    public Guid BookingId { get; init; }
}

public sealed class PaymentGatewayChargeResult
{
    public bool Succeeded { get; init; }

    public string? Provider { get; init; }

    public string? ProviderTransactionId { get; init; }

    public string? CardBrand { get; init; }

    public string? CardLast4 { get; init; }

    public string? FailureReason { get; init; }
}

public sealed class PaymentGatewayRefundRequest
{
    public string? ProviderTransactionId { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; } = string.Empty;

    public Guid PaymentId { get; init; }
}

public sealed class PaymentGatewayRefundResult
{
    public bool Succeeded { get; init; }

    public string? ProviderRefundId { get; init; }

    public string? FailureReason { get; init; }
}

public interface IPaymentGateway
{
    Task<PaymentGatewayChargeResult> ChargeAsync(
        PaymentGatewayChargeRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentGatewayRefundResult> RefundAsync(
        PaymentGatewayRefundRequest request,
        CancellationToken cancellationToken = default);
}
