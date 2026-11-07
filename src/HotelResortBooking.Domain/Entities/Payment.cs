namespace HotelResortBooking.Domain.Entities;

using HotelResortBooking.Domain.Common;
using HotelResortBooking.Domain.Enums;

public sealed class Payment : AuditableEntity
{
    public Guid BookingId { get; set; }

    /// <summary>
    /// Null for guest checkout payments.
    /// </summary>
    public Guid? UserId { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = string.Empty;

    public PaymentMethodType PaymentMethod { get; set; }

    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public string? Provider { get; set; }

    public string? ProviderTransactionId { get; set; }

    public string? PaymentMethodToken { get; set; }

    public string? CardBrand { get; set; }

    public string? CardLast4 { get; set; }

    public string? FailureReason { get; set; }

    public DateTimeOffset? PaidAt { get; set; }

    public Booking Booking { get; set; } = null!;

    public User? User { get; set; }
}
