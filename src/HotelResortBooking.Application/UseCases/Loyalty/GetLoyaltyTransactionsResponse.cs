using HotelResortBooking.Application.Abstractions.Repositories;

namespace HotelResortBooking.Application.UseCases.Loyalty;

public sealed class GetLoyaltyTransactionsResponse
{
    public IReadOnlyList<LoyaltyTransactionData> Transactions { get; init; } = Array.Empty<LoyaltyTransactionData>();
}
