using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Loyalty;

public interface IGetLoyaltyBalanceUseCase
{
    Task<Result<GetLoyaltyBalanceResponse>> ExecuteAsync(CancellationToken cancellationToken = default);
}
