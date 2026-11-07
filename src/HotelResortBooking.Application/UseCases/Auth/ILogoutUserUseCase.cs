using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Auth;

public interface ILogoutUserUseCase
{
    Task<Result<LogoutUserResponse>> ExecuteAsync(CancellationToken cancellationToken = default);
}
