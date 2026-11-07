using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Auth;

public interface IRegisterUserUseCase
{
    Task<Result<RegisterUserResponse>> ExecuteAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default);
}
