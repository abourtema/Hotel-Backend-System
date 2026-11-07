using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Auth;

public sealed class ContinueAsGuestUseCase : IContinueAsGuestUseCase
{
    public Task<Result<ContinueAsGuestResponse>> ExecuteAsync(
        ContinueAsGuestRequest request,
        CancellationToken cancellationToken = default)
    {
        // Guest mode is session-only; no persistent user is created.
        var response = Result.Success(new ContinueAsGuestResponse
        {
            AccessMode = AccessMode.Guest
        });

        return Task.FromResult(response);
    }
}
