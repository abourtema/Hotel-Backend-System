using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Reviews;

public interface IAddReviewUseCase
{
    Task<Result<AddReviewResponse>> ExecuteAsync(
        AddReviewRequest request,
        CancellationToken cancellationToken = default);
}
