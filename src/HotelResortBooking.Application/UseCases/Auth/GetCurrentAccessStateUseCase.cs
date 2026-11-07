using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Auth;

public sealed class GetCurrentAccessStateUseCase : IGetCurrentAccessStateUseCase
{
    private readonly IAccessContext _accessContext;

    public GetCurrentAccessStateUseCase(IAccessContext accessContext)
    {
        _accessContext = accessContext;
    }

    public Task<Result<GetCurrentAccessStateResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var response = Result.Success(new GetCurrentAccessStateResponse
        {
            AccessMode = _accessContext.Mode,
            UserId = _accessContext.UserId,
            IsAuthenticated = _accessContext.IsAuthenticated
        });

        return Task.FromResult(response);
    }
}
