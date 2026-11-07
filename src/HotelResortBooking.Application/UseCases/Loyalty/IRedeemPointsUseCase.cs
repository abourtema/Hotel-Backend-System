using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Loyalty;

public interface IRedeemPointsUseCase
{
    Task<Result<RedeemPointsResponse>> ExecuteAsync(
        RedeemPointsRequest request,
        CancellationToken cancellationToken = default);
}
