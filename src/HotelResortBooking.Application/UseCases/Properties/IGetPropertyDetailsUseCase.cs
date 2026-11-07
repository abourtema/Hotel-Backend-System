using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Properties;

public interface IGetPropertyDetailsUseCase
{
    Task<Result<GetPropertyDetailsResponse>> ExecuteAsync(
        GetPropertyDetailsRequest request,
        CancellationToken cancellationToken = default);
}
