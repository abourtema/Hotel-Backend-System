using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Loyalty;

public interface IRefundPointsUseCase
{
    Task<Result<RefundPointsResponse>> ExecuteAsync(
        RefundPointsRequest request,
        CancellationToken cancellationToken = default);
}
