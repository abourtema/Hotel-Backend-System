using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Search;

public interface ISearchPropertiesUseCase
{
    Task<Result<SearchPropertiesResponse>> ExecuteAsync(
        SearchPropertiesRequest request,
        CancellationToken cancellationToken = default);
}
