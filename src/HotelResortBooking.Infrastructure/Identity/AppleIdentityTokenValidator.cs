using HotelResortBooking.Application.Abstractions.Security;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Application.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace HotelResortBooking.Infrastructure.Identity;

public sealed class AppleIdentityTokenValidator : IAppleIdentityTokenValidator
{
    public const string MetadataAddress = "https://appleid.apple.com/.well-known/openid-configuration";
    public const string Issuer = "https://appleid.apple.com";

    private readonly IOptions<AppleAuthOptions> _options;
    private readonly IOidcConfigurationProvider _configuration;
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly ILogger<AppleIdentityTokenValidator> _logger;

    public AppleIdentityTokenValidator(
        IOptions<AppleAuthOptions> options,
        IOidcConfigurationProvider configuration,
        ILogger<AppleIdentityTokenValidator> logger,
        IHttpClientFactory? httpClientFactory = null)
    {
        _options = options;
        _configuration = configuration;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Result<ExternalIdentity>> ValidateAsync(
        string identityToken,
        CancellationToken cancellationToken = default)
    {
        var audiences = _options.Value.GetAudiences();
        if (audiences.Count == 0)
        {
            _logger.LogWarning("Apple authentication is not configured.");
            return Result.Failure<ExternalIdentity>(BusinessErrors.ProviderUnavailable());
        }

        IReadOnlyList<SecurityKey> signingKeys;
        try
        {
            signingKeys = await JwksSigningKeyLoader.LoadAsync(
                _configuration,
                _httpClientFactory,
                JwksSigningKeyLoader.AppleJwksUrl,
                _logger,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Apple identity provider signing keys could not be retrieved.");
            return Result.Failure<ExternalIdentity>(BusinessErrors.ProviderUnavailable());
        }

        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = Issuer,
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
            identityToken,
            parameters,
            ExternalAuthProviders.Apple,
            BusinessErrors.InvalidAppleToken,
            BusinessErrors.ExpiredAppleToken);

        if (result.IsFailure)
        {
            _logger.LogWarning("Apple identity token validation failed with {ErrorCode}.", result.Error!.Code);
        }

        return result;
    }
}
