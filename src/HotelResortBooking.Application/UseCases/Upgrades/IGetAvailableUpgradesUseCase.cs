using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Upgrades;

public interface IGetAvailableUpgradesUseCase
{
    Task<Result<GetAvailableUpgradesResponse>> ExecuteAsync(
        GetAvailableUpgradesRequest request,
        CancellationToken cancellationToken = default);
}
