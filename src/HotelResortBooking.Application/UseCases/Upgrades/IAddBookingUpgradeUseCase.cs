using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Upgrades;

public interface IAddBookingUpgradeUseCase
{
    Task<Result<AddBookingUpgradeResponse>> ExecuteAsync(
        AddBookingUpgradeRequest request,
        CancellationToken cancellationToken = default);
}
