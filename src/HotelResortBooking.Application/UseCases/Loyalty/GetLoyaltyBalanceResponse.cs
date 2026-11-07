namespace HotelResortBooking.Application.UseCases.Loyalty;

public sealed class GetLoyaltyBalanceResponse
{
    public Guid AccountId { get; init; }

    public int CurrentBalance { get; init; }

    public int LifetimeEarned { get; init; }

    public int LifetimeRedeemed { get; init; }
}
