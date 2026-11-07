using HotelResortBooking.Application.Abstractions.Security;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Application.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HotelResortBooking.Infrastructure.Identity;

public sealed class GoogleIdTokenValidator : IGoogleIdTokenValidator
{
    public const string MetadataAddress = "https://accounts.google.com/.well-known/openid-configuration";

    private static readonly string[] Issuers =
    [
        "https://accounts.google.com",
        "accounts.google.com"
    ];

    private readonly IOptions<GoogleAuthOptions> _options;
    private readonly IOidcConfigurationProvider _configuration;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly ILogger<GoogleIdTokenValidator> _logger;

    public GoogleIdTokenValidator(
        IOptions<GoogleAuthOptions> options,
        IOidcConfigurationProvider configuration,
        ILogger<GoogleIdTokenValidator> logger,
        IHttpClientFactory? httpClientFactory = null)
    {
        _options = options;
        _configuration = configuration;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Result<ExternalIdentity>> ValidateAsync(
        string idToken,
        CancellationToken cancellationToken = default)
    {
        var audiences = _options.Value.GetAudiences();
        if (audiences.Count == 0)
        {
            _logger.LogWarning("Google authentication is not configured.");
            return Result.Failure<ExternalIdentity>(BusinessErrors.ProviderUnavailable());
        }

        IReadOnlyList<SecurityKey> signingKeys;
        try
        {
            signingKeys = await JwksSigningKeyLoader.LoadAsync(
                _configuration,
                _httpClientFactory,
                JwksSigningKeyLoader.GoogleJwksUrl,
                _logger,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Google identity provider signing keys could not be retrieved.");
            return Result.Failure<ExternalIdentity>(BusinessErrors.ProviderUnavailable());
        }

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = Issuers,
            ValidateAudience = true,
            ValidAudiences = audiences,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = signingKeys,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
            RequireExpirationTime = true,
            RequireSignedTokens = true
        };

        var result = IdentityTokenValidator.Validate(
            idToken,
            parameters,
            ExternalAuthProviders.Google,
            BusinessErrors.InvalidGoogleToken,
            BusinessErrors.ExpiredGoogleToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Google identity token validation failed with {ErrorCode}.", result.Error!.Code);
        }

        return result;
    }
}
