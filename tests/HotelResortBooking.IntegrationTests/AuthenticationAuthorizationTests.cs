using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HotelResortBooking.Api.Contracts.Auth;
using Xunit;

namespace HotelResortBooking.IntegrationTests;

public sealed class AuthenticationAuthorizationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;

    public AuthenticationAuthorizationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Search_AllowsAnonymous()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/hotels/search?destination=&adults=2&rooms=1&checkInDate=2026-09-01&checkOutDate=2026-09-03");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Bookings_Anonymous_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/bookings");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Register_Login_And_GetCurrentUser()
    {
        var client = _factory.CreateClient();
        var email = $"user-{Guid.NewGuid():N}@example.com";

        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "Password1!",
            FirstName = "Ada",
            LastName = "Lovelace"
        });

        Assert.Equal(HttpStatusCode.Created, register.StatusCode);
        var registered = await register.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.False(string.IsNullOrWhiteSpace(registered?.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(registered.RefreshToken));

        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = email,
            Password = "Password1!"
        });

        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var loggedIn = await login.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(loggedIn);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loggedIn.AccessToken);
        var me = await client.GetAsync("/api/v1/auth/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);

        var account = await client.GetAsync("/api/v1/account");
        Assert.Equal(HttpStatusCode.OK, account.StatusCode);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest
        {
            Email = "nobody@example.com",
            Password = "Password1!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_And_Revoke()
    {
        var client = _factory.CreateClient();
        var email = $"refresh-{Guid.NewGuid():N}@example.com";

        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "Password1!",
            FirstName = "Ada",
            LastName = "Lovelace"
        });
        var session = await register.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(session);

        var refreshedResponse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = session.RefreshToken
        });
        Assert.Equal(HttpStatusCode.OK, refreshedResponse.StatusCode);
        var refreshed = await refreshedResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();
        Assert.NotNull(refreshed);
        Assert.NotEqual(session.RefreshToken, refreshed.RefreshToken);

        var revoke = await client.PostAsJsonAsync("/api/v1/auth/revoke", new RevokeTokenRequest
        {
            RefreshToken = refreshed.RefreshToken
        });
        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);

        var reuse = await client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshTokenRequest
        {
            RefreshToken = refreshed.RefreshToken
        });
        Assert.Equal(HttpStatusCode.Unauthorized, reuse.StatusCode);
    }

    [Fact]
    public async Task AdminPing_UserToken_ReturnsForbidden()
    {
        var client = _factory.CreateClient();
        var email = $"member-{Guid.NewGuid():N}@example.com";
        var register = await client.PostAsJsonAsync("/api/v1/auth/register", new RegisterRequest
        {
            Email = email,
            Password = "Password1!",
            FirstName = "Ada",
            LastName = "Lovelace"
        });
        var session = await register.Content.ReadFromJsonAsync<AuthenticationResponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session!.AccessToken);

        var response = await client.GetAsync("/api/v1/admin/ping");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GoogleLogin_Unconfigured_ReturnsProviderUnavailable()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/google", new GoogleLoginRequest
        {
            IdToken = "not-a-real-token"
        });

        Assert.True(
            response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.UnprocessableEntity,
            $"Unexpected status {response.StatusCode}");
    }

    [Fact]
    public async Task AppleLogin_Unconfigured_ReturnsProviderUnavailable()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/auth/apple", new AppleLoginRequest
        {
            IdentityToken = "not-a-real-token"
        });

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Guest_StillWorks_AfterSocialAuthEndpoints()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsync("/api/v1/auth/guest", content: null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
