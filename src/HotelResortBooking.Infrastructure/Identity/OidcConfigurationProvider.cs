using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace HotelResortBooking.Infrastructure.Identity;

public interface IOidcConfigurationProvider
{
    Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default);
}

public class CachedOidcConfigurationProvider : IOidcConfigurationProvider
{
    private readonly ConfigurationManager<OpenIdConnectConfiguration> _manager;

    public CachedOidcConfigurationProvider(string metadataAddress)
    {
        _manager = new ConfigurationManager<OpenIdConnectConfiguration>(
            metadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever { RequireHttps = true });
    }

    public Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default)
        => _manager.GetConfigurationAsync(cancellationToken);
}

public sealed class StaticOidcConfigurationProvider : IOidcConfigurationProvider
{
    private readonly OpenIdConnectConfiguration _configuration;

    public StaticOidcConfigurationProvider(OpenIdConnectConfiguration configuration)
    {
        _configuration = configuration;
    }

    public Task<OpenIdConnectConfiguration> GetConfigurationAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_configuration);
}

public sealed class GoogleOidcConfigurationProvider : CachedOidcConfigurationProvider
{
    public GoogleOidcConfigurationProvider()
        : base(GoogleIdTokenValidator.MetadataAddress)
    {
    }
}

public sealed class AppleOidcConfigurationProvider : CachedOidcConfigurationProvider
{
    public AppleOidcConfigurationProvider()
        : base(AppleIdentityTokenValidator.MetadataAddress)
    {
    }
}
