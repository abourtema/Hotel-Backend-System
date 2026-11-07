using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Auth;

public sealed class LogoutUserUseCase : ILogoutUserUseCase
{
    public Task<Result<LogoutUserResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        // Business contract only — JWT/session clearing is handled by the API layer later.
        var response = Result.Success(new LogoutUserResponse
        {
            Success = true,
            AccessMode = AccessMode.Initial
        });

        return Task.FromResult(response);
    }
}
