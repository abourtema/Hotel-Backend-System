using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Map;

public interface ISearchPropertiesByLocationUseCase
{
    Task<Result<SearchPropertiesByLocationResponse>> ExecuteAsync(
        SearchPropertiesByLocationRequest request,
        CancellationToken cancellationToken = default);
}
