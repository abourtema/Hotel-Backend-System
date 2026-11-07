using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Services;
using HotelResortBooking.Application.UseCases.Auth;
using HotelResortBooking.UnitTests.Fakes;
using Xunit;

namespace HotelResortBooking.UnitTests.Services;

public sealed class AuthenticationServiceTests
{
    [Fact]
    public async Task Register_ValidRequest_IssuesTokens()
    {
        var sut = CreateSut(out var users, out var refreshTokens, out _, out _);

        var result = await sut.RegisterAsync(new RegisterUserRequest
        {
            Email = "guest@example.com",
            Password = "Password1!",
            FirstName = "Ada",
            LastName = "Lovelace"
        }, "127.0.0.1");

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.Value.RefreshToken));
        Assert.Single(users.Users);
        Assert.Single(refreshTokens.Tokens);
        Assert.DoesNotContain("Password", result.Value.AccessToken, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Register_WeakPassword_Fails()
    {
        var sut = CreateSut(out _, out _, out _, out _);

        var result = await sut.RegisterAsync(new RegisterUserRequest
        {
            Email = "guest@example.com",
            Password = "short",
            FirstName = "Ada",
            LastName = "Lovelace"
        }, null);

        Assert.True(result.IsFailure);
        Assert.Equal("WeakPassword", result.Error!.Code);
    }

    [Fact]
    public async Task Login_InvalidCredentials_Fails()
    {
        var sut = CreateSut(out _, out _, out _, out _);

        var result = await sut.LoginAsync(new LoginUserRequest
        {
            Email = "missing@example.com",
            Password = "Password1!"
        }, null);

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidCredentials", result.Error!.Code);
    }

    [Fact]
    public async Task Login_ValidCredentials_IssuesTokens()
    {
        var sut = CreateSut(out _, out var refreshTokens, out var hasher, out var users);
        var created = await users.CreateAsync("user@example.com", hasher.Hash("Password1!"), "Ada", "Lovelace", null);

        var result = await sut.LoginAsync(new LoginUserRequest
        {
            Email = created.Email,
            Password = "Password1!"
        }, "127.0.0.1");

        Assert.True(result.IsSuccess);
        Assert.Equal(created.Id, result.Value.UserId);
        Assert.Single(refreshTokens.Tokens);
    }

    [Fact]
    public async Task Refresh_RotatesToken()
    {
        var sut = CreateSut(out _, out var refreshTokens, out var hasher, out var users);
        var created = await users.CreateAsync("user@example.com", hasher.Hash("Password1!"), "Ada", "Lovelace", null);
        var login = await sut.LoginAsync(new LoginUserRequest { Email = created.Email, Password = "Password1!" }, null);
        Assert.True(login.IsSuccess, login.Error?.Message);
        var refreshed = await sut.RefreshAsync(login.Value.RefreshToken, "127.0.0.1");

        Assert.True(refreshed.IsSuccess);
        Assert.NotEqual(login.Value.RefreshToken, refreshed.Value.RefreshToken);
        Assert.Equal(2, refreshTokens.Tokens.Count);
        Assert.True(refreshTokens.Tokens[0].IsRevoked);
    }

    [Fact]
    public async Task Refresh_RevokedToken_FailsAndRevokesFamily()
    {
        var sut = CreateSut(out _, out var refreshTokens, out var hasher, out var users);
        var created = await users.CreateAsync("user@example.com", hasher.Hash("Password1!"), "Ada", "Lovelace", null);
        var login = await sut.LoginAsync(new LoginUserRequest { Email = created.Email, Password = "Password1!" }, null);
        await sut.RevokeAsync(login.Value.RefreshToken, "127.0.0.1");

        var result = await sut.RefreshAsync(login.Value.RefreshToken, "127.0.0.1");

        Assert.True(result.IsFailure);
        Assert.Equal("RevokedRefreshToken", result.Error!.Code);
        Assert.All(refreshTokens.Tokens, token => Assert.True(token.IsRevoked));
    }

    [Fact]
    public async Task Refresh_ExpiredToken_Fails()
    {
        var sut = CreateSut(out _, out var refreshTokens, out var hasher, out var users);
        var created = await users.CreateAsync("user@example.com", hasher.Hash("Password1!"), "Ada", "Lovelace", null);
        var login = await sut.LoginAsync(new LoginUserRequest { Email = created.Email, Password = "Password1!" }, null);
        refreshTokens.Tokens[0].ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);

        var result = await sut.RefreshAsync(login.Value.RefreshToken, "127.0.0.1");

        Assert.True(result.IsFailure);
        Assert.Equal("ExpiredRefreshToken", result.Error!.Code);
    }

    private static AuthenticationService CreateSut(
        out FakeUserRepository users,
        out FakeRefreshTokenRepository refreshTokens,
        out FakePasswordHasher hasher,
        out FakeUserRepository usersAlias)
    {
        users = new FakeUserRepository();
        usersAlias = users;
        refreshTokens = new FakeRefreshTokenRepository();
        hasher = new FakePasswordHasher();
        var access = new FakeAccessContext();
        var clock = new FakeDateTimeProvider();
        var unitOfWork = new FakeUnitOfWork();
        var tokens = new FakeTokenService();

        var register = new RegisterUserUseCase(users, hasher, unitOfWork);
        var login = new LoginUserUseCase(users, hasher, clock, unitOfWork);
        var logout = new LogoutUserUseCase();
        var socialLogin = new SocialLoginUseCase(users, new FakeExternalLoginRepository(), hasher, clock, unitOfWork);

        return new AuthenticationService(
            register,
            login,
            logout,
            socialLogin,
            new FakeGoogleIdTokenValidator(),
            new FakeAppleIdentityTokenValidator(),
            users,
            refreshTokens,
            tokens,
            access,
            clock,
            unitOfWork,
            new FakeRolePermissionRepository());
    }
}
