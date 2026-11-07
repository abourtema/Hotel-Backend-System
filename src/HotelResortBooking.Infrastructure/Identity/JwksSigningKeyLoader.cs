using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace HotelResortBooking.Infrastructure.Identity;

internal static class JwksSigningKeyLoader
{
    public const string GoogleJwksUrl = "https://www.googleapis.com/oauth2/v3/certs";
    public const string AppleJwksUrl = "https://appleid.apple.com/auth/keys";

    public static async Task<IReadOnlyList<SecurityKey>> LoadAsync(
        IOidcConfigurationProvider configuration,
        IHttpClientFactory? httpClientFactory,
        string jwksFallbackUrl,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            var oidc = await configuration.GetConfigurationAsync(cancellationToken);
            if (oidc.SigningKeys.Count > 0)
            {
                return oidc.SigningKeys.ToList();
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "OpenID metadata could not be retrieved; trying JWKS fallback.");
        }

        if (httpClientFactory is null)
        {
            throw new InvalidOperationException("Identity provider signing keys are unavailable.");
        }

        var client = httpClientFactory.CreateClient("oidc-jwks");
        using var response = await client.GetAsync(jwksFallbackUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return new JsonWebKeySet(json).GetSigningKeys().ToList();
    }
}

internal static class HttpClientJwksExtensions
{
    public static IHttpClientBuilder AddOidcJwksClient(this IServiceCollection services)
    {
        return services.AddHttpClient("oidc-jwks", client =>
        {
            client.Timeout = TimeSpan.FromSeconds(10);
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("HotelResortBooking", "1.0"));
        });
    }
}
