namespace HotelResortBooking.Domain.Entities;

using HotelResortBooking.Domain.Common;

public sealed class PaymentMethod : AuditableEntity
{
    public Guid UserId { get; set; }

    public string Provider { get; set; } = string.Empty;

    public string ProviderPaymentMethodId { get; set; } = string.Empty;

    public string? CardBrand { get; set; }

    public string? CardLast4 { get; set; }

    public int? ExpiryMonth { get; set; }

    public int? ExpiryYear { get; set; }

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; } = true;

    public User User { get; set; } = null!;
}
