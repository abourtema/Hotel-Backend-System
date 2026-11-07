using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Auth;

public interface IContinueAsGuestUseCase
{
    Task<Result<ContinueAsGuestResponse>> ExecuteAsync(
        ContinueAsGuestRequest request,
        CancellationToken cancellationToken = default);
}
