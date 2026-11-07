using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.UseCases.Payments;

public sealed class GetPaymentResponse
{
    public Guid PaymentId { get; init; }

    public Guid BookingId { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; } = string.Empty;

    public PaymentMethodType PaymentMethod { get; init; }

    public PaymentStatus Status { get; init; }

    public string? Provider { get; init; }

    public string? ProviderTransactionId { get; init; }

    public string? CardBrand { get; init; }

    public string? CardLast4 { get; init; }

    public string? FailureReason { get; init; }

    public DateTimeOffset? PaidAt { get; init; }

    public DateTimeOffset CreatedAt { get; init; }
}
