using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Loyalty;

public interface ICalculatePointsRedemptionUseCase
{
    Task<Result<CalculatePointsRedemptionResponse>> ExecuteAsync(
        CalculatePointsRedemptionRequest request,
        CancellationToken cancellationToken = default);
}
