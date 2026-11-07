namespace HotelResortBooking.Domain.Entities;

using HotelResortBooking.Domain.Common;
using HotelResortBooking.Domain.Enums;

public sealed class LoyaltyTransaction : Entity
{
    public Guid LoyaltyAccountId { get; set; }

    public Guid? BookingId { get; set; }

    public LoyaltyTransactionType TransactionType { get; set; }

    public int Points { get; set; }

    public int BalanceAfter { get; set; }

    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public LoyaltyAccount LoyaltyAccount { get; set; } = null!;

    public Booking? Booking { get; set; }
}
