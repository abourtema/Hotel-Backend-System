using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Search;

public interface IGetPropertySearchResultsUseCase
{
    Task<Result<SearchPropertiesResponse>> ExecuteAsync(
        SearchPropertiesRequest request,
        CancellationToken cancellationToken = default);
}
