using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Auth;

public interface IGetCurrentAccessStateUseCase
{
    Task<Result<GetCurrentAccessStateResponse>> ExecuteAsync(CancellationToken cancellationToken = default);
}
