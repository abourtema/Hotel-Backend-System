using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Upgrades;

public sealed class GetAvailableUpgradesUseCase : IGetAvailableUpgradesUseCase
{
    private readonly IPropertyRepository _propertyRepository;
    private readonly IUpgradeRepository _upgradeRepository;

    public GetAvailableUpgradesUseCase(
        IPropertyRepository propertyRepository,
        IUpgradeRepository upgradeRepository)
    {
        _propertyRepository = propertyRepository;
        _upgradeRepository = upgradeRepository;
    }

    public async Task<Result<GetAvailableUpgradesResponse>> ExecuteAsync(
        GetAvailableUpgradesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!await _propertyRepository.ExistsActiveAsync(request.PropertyId, cancellationToken))
        {
            return Result.Failure<GetAvailableUpgradesResponse>(BusinessErrors.PropertyNotFound(request.PropertyId));
        }

        var upgrades = await _upgradeRepository.GetForPropertyAsync(
            request.PropertyId,
            request.RoomTypeId,
            cancellationToken);

        return Result.Success(new GetAvailableUpgradesResponse
        {
            Upgrades = upgrades
        });
    }
}
