using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Auth;

public interface ILoginUserUseCase
{
    Task<Result<LoginUserResponse>> ExecuteAsync(
        LoginUserRequest request,
        CancellationToken cancellationToken = default);
}
