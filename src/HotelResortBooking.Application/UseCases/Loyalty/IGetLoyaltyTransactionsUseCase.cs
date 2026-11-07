using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Loyalty;

public interface IGetLoyaltyTransactionsUseCase
{
    Task<Result<GetLoyaltyTransactionsResponse>> ExecuteAsync(CancellationToken cancellationToken = default);
}
