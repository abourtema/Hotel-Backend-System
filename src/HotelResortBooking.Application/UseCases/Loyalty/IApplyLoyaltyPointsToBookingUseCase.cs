using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Loyalty;

public interface IApplyLoyaltyPointsToBookingUseCase
{
    Task<Result<ApplyLoyaltyPointsToBookingResponse>> ExecuteAsync(
        ApplyLoyaltyPointsToBookingRequest request,
        CancellationToken cancellationToken = default);
}
