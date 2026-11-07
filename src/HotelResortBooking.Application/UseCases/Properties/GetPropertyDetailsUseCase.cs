using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Properties;

public sealed class GetPropertyDetailsUseCase : IGetPropertyDetailsUseCase
{
    private readonly IPropertyRepository _propertyRepository;

    public GetPropertyDetailsUseCase(IPropertyRepository propertyRepository)
    {
        _propertyRepository = propertyRepository;
    }

    public async Task<Result<GetPropertyDetailsResponse>> ExecuteAsync(
        GetPropertyDetailsRequest request,
        CancellationToken cancellationToken = default)
    {
        var details = await _propertyRepository.GetDetailsAsync(request.PropertyId, cancellationToken);
        if (details is null)
        {
            return Result.Failure<GetPropertyDetailsResponse>(BusinessErrors.PropertyNotFound(request.PropertyId));
        }

        if (!await _propertyRepository.ExistsActiveAsync(request.PropertyId, cancellationToken))
        {
            return Result.Failure<GetPropertyDetailsResponse>(BusinessErrors.PropertyInactive(request.PropertyId));
        }

        return Result.Success(Map(details));
    }

    private static GetPropertyDetailsResponse Map(PropertyDetailsData details) => new()
    {
        Id = details.Id,
        Name = details.Name,
        Description = details.Description,
        PropertyType = details.PropertyType,
        StarRating = details.StarRating,
        ReviewScore = details.ReviewScore,
        ReviewCount = details.ReviewCount,
        AddressLine = details.AddressLine,
        City = details.City,
        Country = details.Country,
        PostalCode = details.PostalCode,
        Latitude = details.Latitude,
        Longitude = details.Longitude,
        CheckInTime = details.CheckInTime,
        CheckOutTime = details.CheckOutTime,
        CancellationPolicy = details.CancellationPolicy,
        DepositPolicy = details.DepositPolicy,
        Images = details.Images,
        Amenities = details.Amenities,
        Rooms = details.Rooms,
        UpgradeOptions = details.UpgradeOptions
    };
}
