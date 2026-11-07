using HotelResortBooking.Application.Abstractions.Security;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.UnitTests.Fakes;

public sealed class FakeGoogleIdTokenValidator : IGoogleIdTokenValidator
{
    public Result<ExternalIdentity> NextResult { get; set; } =
        Result.Failure<ExternalIdentity>(BusinessErrors.InvalidGoogleToken());

    public Task<Result<ExternalIdentity>> ValidateAsync(
        string idToken,
        CancellationToken cancellationToken = default)
        => Task.FromResult(NextResult);
}

public sealed class FakeAppleIdentityTokenValidator : IAppleIdentityTokenValidator
{
    public Result<ExternalIdentity> NextResult { get; set; } =
        Result.Failure<ExternalIdentity>(BusinessErrors.InvalidAppleToken());

    public Task<Result<ExternalIdentity>> ValidateAsync(
        string identityToken,
        CancellationToken cancellationToken = default)
        => Task.FromResult(NextResult);
}
