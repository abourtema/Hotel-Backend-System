using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Upgrades;

public interface IRemoveBookingUpgradeUseCase
{
    Task<Result<RemoveBookingUpgradeResponse>> ExecuteAsync(
        RemoveBookingUpgradeRequest request,
        CancellationToken cancellationToken = default);
}
