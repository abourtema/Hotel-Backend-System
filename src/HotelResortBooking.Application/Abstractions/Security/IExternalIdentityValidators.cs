using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.Abstractions.Security;

public sealed class ExternalIdentity
{
    public string Provider { get; init; } = string.Empty;

    public string ProviderUserId { get; init; } = string.Empty;

    public string? Email { get; init; }

    public bool EmailVerified { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }
}

public interface IGoogleIdTokenValidator
{
    Task<Result<ExternalIdentity>> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}

public interface IAppleIdentityTokenValidator
{
    Task<Result<ExternalIdentity>> ValidateAsync(string identityToken, CancellationToken cancellationToken = default);
}
