using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using HotelResortBooking.Application.Abstractions.Security;
using HotelResortBooking.Infrastructure.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace HotelResortBooking.UnitTests.Security;

public sealed class SocialTokenValidatorTests
{
    [Fact]
    public async Task Google_ValidToken_ReturnsSubjectAndEmail()
    {
        using var rsa = RSA.Create(2048);
        var token = CreateToken(
            rsa,
            issuer: "https://accounts.google.com",
            audience: "google-client-id",
            subject: "google-sub-1",
            expires: DateTime.UtcNow.AddMinutes(10),
            extra: [new Claim("email", "ada@gmail.com"), new Claim("given_name", "Ada"), new Claim("family_name", "Lovelace")]);

        var validator = CreateGoogleValidator(rsa, "google-client-id");
        var result = await validator.ValidateAsync(token);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal("google-sub-1", result.Value.ProviderUserId);
        Assert.Equal("ada@gmail.com", result.Value.Email);
        Assert.Equal("Ada", result.Value.FirstName);
        Assert.Equal("Lovelace", result.Value.LastName);
        Assert.Equal("Google", result.Value.Provider);
    }

    [Fact]
    public async Task Google_InvalidSignature_Fails()
    {
        using var signing = RSA.Create(2048);
        using var other = RSA.Create(2048);
        var token = CreateToken(signing, "https://accounts.google.com", "google-client-id", "google-sub-1", DateTime.UtcNow.AddMinutes(10));

        var validator = CreateGoogleValidator(other, "google-client-id");
        var result = await validator.ValidateAsync(token);

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidGoogleToken", result.Error!.Code);
    }

    [Fact]
    public async Task Google_ExpiredToken_Fails()
    {
        using var rsa = RSA.Create(2048);
        var token = CreateToken(
            rsa,
            "https://accounts.google.com",
            "google-client-id",
            "google-sub-1",
            DateTime.UtcNow.AddMinutes(-10),
            notBefore: DateTime.UtcNow.AddMinutes(-20));

        var validator = CreateGoogleValidator(rsa, "google-client-id");
        var result = await validator.ValidateAsync(token);

        Assert.True(result.IsFailure);
        Assert.Equal("ExpiredGoogleToken", result.Error!.Code);
    }

    [Fact]
    public async Task Google_InvalidAudience_Fails()
    {
        using var rsa = RSA.Create(2048);
        var token = CreateToken(rsa, "https://accounts.google.com", "other-client", "google-sub-1", DateTime.UtcNow.AddMinutes(10));

        var validator = CreateGoogleValidator(rsa, "google-client-id");
        var result = await validator.ValidateAsync(token);

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidAudience", result.Error!.Code);
    }

    [Fact]
    public async Task Google_InvalidIssuer_Fails()
    {
        using var rsa = RSA.Create(2048);
        var token = CreateToken(rsa, "https://evil.example", "google-client-id", "google-sub-1", DateTime.UtcNow.AddMinutes(10));

        var validator = CreateGoogleValidator(rsa, "google-client-id");
        var result = await validator.ValidateAsync(token);

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidIssuer", result.Error!.Code);
    }

    [Fact]
    public async Task Google_MissingClientId_IsUnavailable()
    {
        using var rsa = RSA.Create(2048);
        var token = CreateToken(rsa, "https://accounts.google.com", "google-client-id", "google-sub-1", DateTime.UtcNow.AddMinutes(10));
        var validator = CreateGoogleValidator(rsa, clientId: "");

        var result = await validator.ValidateAsync(token);

        Assert.True(result.IsFailure);
        Assert.Equal("ProviderUnavailable", result.Error!.Code);
    }

    [Fact]
    public async Task Apple_ValidToken_ReturnsSubject()
    {
        using var rsa = RSA.Create(2048);
        var token = CreateToken(
            rsa,
            issuer: AppleIdentityTokenValidator.Issuer,
            audience: "com.oya.hotels",
            subject: "apple-sub-1",
            expires: DateTime.UtcNow.AddMinutes(10),
            extra: [new Claim("email", "relay@privaterelay.appleid.com"), new Claim("email_verified", "true")]);

        var validator = CreateAppleValidator(rsa, "com.oya.hotels");
        var result = await validator.ValidateAsync(token);

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal("apple-sub-1", result.Value.ProviderUserId);
        Assert.Equal("relay@privaterelay.appleid.com", result.Value.Email);
        Assert.True(result.Value.EmailVerified);
        Assert.Equal("Apple", result.Value.Provider);
    }

    [Fact]
    public async Task Apple_InvalidSignature_Fails()
    {
        using var signing = RSA.Create(2048);
        using var other = RSA.Create(2048);
        var token = CreateToken(signing, AppleIdentityTokenValidator.Issuer, "com.oya.hotels", "apple-sub-1", DateTime.UtcNow.AddMinutes(10));

        var validator = CreateAppleValidator(other, "com.oya.hotels");
        var result = await validator.ValidateAsync(token);

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidAppleToken", result.Error!.Code);
    }

    [Fact]
    public async Task Apple_ExpiredToken_Fails()
    {
        using var rsa = RSA.Create(2048);
        var token = CreateToken(
            rsa,
            AppleIdentityTokenValidator.Issuer,
            "com.oya.hotels",
            "apple-sub-1",
            DateTime.UtcNow.AddMinutes(-10),
            notBefore: DateTime.UtcNow.AddMinutes(-20));

        var validator = CreateAppleValidator(rsa, "com.oya.hotels");
        var result = await validator.ValidateAsync(token);

        Assert.True(result.IsFailure);
        Assert.Equal("ExpiredAppleToken", result.Error!.Code);
    }

    [Fact]
    public async Task Apple_InvalidAudience_Fails()
    {
        using var rsa = RSA.Create(2048);
        var token = CreateToken(rsa, AppleIdentityTokenValidator.Issuer, "com.other.app", "apple-sub-1", DateTime.UtcNow.AddMinutes(10));

        var validator = CreateAppleValidator(rsa, "com.oya.hotels");
        var result = await validator.ValidateAsync(token);

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidAudience", result.Error!.Code);
    }

    [Fact]
    public async Task Apple_InvalidIssuer_Fails()
    {
        using var rsa = RSA.Create(2048);
        var token = CreateToken(rsa, "https://evil.example", "com.oya.hotels", "apple-sub-1", DateTime.UtcNow.AddMinutes(10));

        var validator = CreateAppleValidator(rsa, "com.oya.hotels");
        var result = await validator.ValidateAsync(token);

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidIssuer", result.Error!.Code);
    }

    private static GoogleIdTokenValidator CreateGoogleValidator(RSA rsa, string clientId)
    {
        var options = Options.Create(new GoogleAuthOptions { ClientId = clientId });
        var config = new OpenIdConnectConfiguration();
        config.SigningKeys.Add(new RsaSecurityKey(rsa.ExportParameters(false)) { KeyId = "test" });
        return new GoogleIdTokenValidator(
            options,
            new StaticOidcConfigurationProvider(config),
            NullLogger<GoogleIdTokenValidator>.Instance);
    }

    private static AppleIdentityTokenValidator CreateAppleValidator(RSA rsa, string clientId)
    {
        var options = Options.Create(new AppleAuthOptions { ClientId = clientId });
        var config = new OpenIdConnectConfiguration();
        config.SigningKeys.Add(new RsaSecurityKey(rsa.ExportParameters(false)) { KeyId = "test" });
        return new AppleIdentityTokenValidator(
            options,
            new StaticOidcConfigurationProvider(config),
            NullLogger<AppleIdentityTokenValidator>.Instance);
    }

    private static string CreateToken(
        RSA rsa,
        string issuer,
        string audience,
        string subject,
        DateTime expires,
        DateTime? notBefore = null,
        IEnumerable<Claim>? extra = null)
    {
        var key = new RsaSecurityKey(rsa) { KeyId = "test" };
        var credentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256);
        var claims = new List<Claim> { new(JwtRegisteredClaimNames.Sub, subject) };
        if (extra is not null)
        {
            claims.AddRange(extra);
        }

        var token = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            notBefore: notBefore ?? DateTime.UtcNow.AddMinutes(-1),
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
