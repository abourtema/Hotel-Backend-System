using HotelResortBooking.Application.Abstractions.Security;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Application.Security;
using HotelResortBooking.Application.Services;
using HotelResortBooking.Application.UseCases.Auth;
using HotelResortBooking.UnitTests.Fakes;
using Xunit;

namespace HotelResortBooking.UnitTests.Services;

public sealed class SocialAuthenticationTests
{
    [Fact]
    public async Task GoogleLogin_ValidToken_NewUser_IssuesTokens()
    {
        var sut = CreateSut(out var users, out var refreshTokens, out var google, out _, out var logins);

        google.NextResult = Result.Success(new ExternalIdentity
        {
            Provider = ExternalAuthProviders.Google,
            ProviderUserId = "google-sub-1",
            Email = "ada@gmail.com",
            EmailVerified = true,
            FirstName = "Ada",
            LastName = "Lovelace"
        });

        var result = await sut.LoginWithGoogleAsync("valid-google-token", "127.0.0.1");

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.Equal("ada@gmail.com", result.Value.Email);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.Value.RefreshToken));
        Assert.Single(users.Users);
        Assert.Single(refreshTokens.Tokens);
        Assert.Single(logins.Logins);
        Assert.Equal(ExternalAuthProviders.Google, logins.Logins[0].Provider);
        Assert.Equal("google-sub-1", logins.Logins[0].ProviderUserId);
    }

    [Fact]
    public async Task GoogleLogin_InvalidToken_Fails()
    {
        var sut = CreateSut(out _, out _, out var google, out _, out _);
        google.NextResult = Result.Failure<ExternalIdentity>(
            HotelResortBooking.Application.Common.Errors.BusinessErrors.InvalidGoogleToken());

        var result = await sut.LoginWithGoogleAsync("invalid-token", null);

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidGoogleToken", result.Error!.Code);
    }

    [Fact]
    public async Task GoogleLogin_ExpiredToken_Fails()
    {
        var sut = CreateSut(out _, out _, out var google, out _, out _);
        google.NextResult = Result.Failure<ExternalIdentity>(
            HotelResortBooking.Application.Common.Errors.BusinessErrors.ExpiredGoogleToken());

        var result = await sut.LoginWithGoogleAsync("expired-token", null);

        Assert.True(result.IsFailure);
        Assert.Equal("ExpiredGoogleToken", result.Error!.Code);
    }

    [Fact]
    public async Task GoogleLogin_InvalidAudience_Fails()
    {
        var sut = CreateSut(out _, out _, out var google, out _, out _);
        google.NextResult = Result.Failure<ExternalIdentity>(
            HotelResortBooking.Application.Common.Errors.BusinessErrors.InvalidAudience());

        var result = await sut.LoginWithGoogleAsync("wrong-aud-token", null);

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidAudience", result.Error!.Code);
    }

    [Fact]
    public async Task GoogleLogin_ExistingLinkedUser_IssuesTokens()
    {
        var sut = CreateSut(out var users, out var refreshTokens, out var google, out _, out var logins);
        var created = await users.CreateAsync("ada@gmail.com", "hash", "Ada", "Lovelace", null);
        await logins.AddAsync(created.Id, ExternalAuthProviders.Google, "google-sub-1");

        google.NextResult = Result.Success(new ExternalIdentity
        {
            Provider = ExternalAuthProviders.Google,
            ProviderUserId = "google-sub-1",
            Email = "ada@gmail.com"
        });

        var result = await sut.LoginWithGoogleAsync("valid-google-token", "127.0.0.1");

        Assert.True(result.IsSuccess);
        Assert.Equal(created.Id, result.Value.UserId);
        Assert.Single(users.Users);
        Assert.Single(logins.Logins);
        Assert.Single(refreshTokens.Tokens);
    }

    [Fact]
    public async Task GoogleLogin_DisabledUser_Fails()
    {
        var sut = CreateSut(out var users, out _, out var google, out _, out var logins);
        var created = await users.CreateAsync("ada@gmail.com", "hash", "Ada", "Lovelace", null);
        users.Replace(new HotelResortBooking.Application.Abstractions.Repositories.UserAccountData
        {
            Id = created.Id,
            Email = created.Email,
            PasswordHash = created.PasswordHash,
            FirstName = created.FirstName,
            LastName = created.LastName,
            PhoneNumber = created.PhoneNumber,
            IsActive = false,
            Role = created.Role
        });
        await logins.AddAsync(created.Id, ExternalAuthProviders.Google, "google-sub-1");

        google.NextResult = Result.Success(new ExternalIdentity
        {
            Provider = ExternalAuthProviders.Google,
            ProviderUserId = "google-sub-1",
            Email = "ada@gmail.com"
        });

        var result = await sut.LoginWithGoogleAsync("valid-google-token", null);

        Assert.True(result.IsFailure);
        Assert.Equal("AccountDisabled", result.Error!.Code);
    }

    [Fact]
    public async Task GoogleLogin_EmailConflictWithPasswordAccount_DoesNotMerge()
    {
        var sut = CreateSut(out var users, out _, out var google, out _, out var logins);
        await users.CreateAsync("ada@gmail.com", "hash:Password1!", "Ada", "Lovelace", null);

        google.NextResult = Result.Success(new ExternalIdentity
        {
            Provider = ExternalAuthProviders.Google,
            ProviderUserId = "google-sub-new",
            Email = "ada@gmail.com",
            FirstName = "Ada",
            LastName = "Lovelace"
        });

        var result = await sut.LoginWithGoogleAsync("valid-google-token", null);

        Assert.True(result.IsFailure);
        Assert.Equal("AccountConflict", result.Error!.Code);
        Assert.Empty(logins.Logins);
        Assert.Single(users.Users);
    }

    [Fact]
    public async Task AppleLogin_ValidToken_NewUserWithoutEmail_IssuesTokens()
    {
        var sut = CreateSut(out var users, out var refreshTokens, out _, out var apple, out var logins);

        apple.NextResult = Result.Success(new ExternalIdentity
        {
            Provider = ExternalAuthProviders.Apple,
            ProviderUserId = "apple-sub-1"
        });

        var result = await sut.LoginWithAppleAsync("valid-apple-token", "127.0.0.1");

        Assert.True(result.IsSuccess, result.Error?.Message);
        Assert.False(string.IsNullOrWhiteSpace(result.Value.AccessToken));
        Assert.Contains("@users.noreply.local", result.Value.Email);
        Assert.Single(users.Users);
        Assert.Single(refreshTokens.Tokens);
        Assert.Equal(ExternalAuthProviders.Apple, logins.Logins[0].Provider);
    }

    [Fact]
    public async Task AppleLogin_InvalidToken_Fails()
    {
        var sut = CreateSut(out _, out _, out _, out var apple, out _);
        apple.NextResult = Result.Failure<ExternalIdentity>(
            HotelResortBooking.Application.Common.Errors.BusinessErrors.InvalidAppleToken());

        var result = await sut.LoginWithAppleAsync("invalid-token", null);

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidAppleToken", result.Error!.Code);
    }

    [Fact]
    public async Task AppleLogin_ExpiredToken_Fails()
    {
        var sut = CreateSut(out _, out _, out _, out var apple, out _);
        apple.NextResult = Result.Failure<ExternalIdentity>(
            HotelResortBooking.Application.Common.Errors.BusinessErrors.ExpiredAppleToken());

        var result = await sut.LoginWithAppleAsync("expired-token", null);

        Assert.True(result.IsFailure);
        Assert.Equal("ExpiredAppleToken", result.Error!.Code);
    }

    [Fact]
    public async Task AppleLogin_InvalidAudience_Fails()
    {
        var sut = CreateSut(out _, out _, out _, out var apple, out _);
        apple.NextResult = Result.Failure<ExternalIdentity>(
            HotelResortBooking.Application.Common.Errors.BusinessErrors.InvalidAudience());

        var result = await sut.LoginWithAppleAsync("wrong-aud-token", null);

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidAudience", result.Error!.Code);
    }

    [Fact]
    public async Task AppleLogin_ExistingLinkedUser_IssuesTokens()
    {
        var sut = CreateSut(out var users, out _, out _, out var apple, out var logins);
        var created = await users.CreateAsync("relay@privaterelay.appleid.com", "hash", "User", "Apple", null);
        await logins.AddAsync(created.Id, ExternalAuthProviders.Apple, "apple-sub-1");

        apple.NextResult = Result.Success(new ExternalIdentity
        {
            Provider = ExternalAuthProviders.Apple,
            ProviderUserId = "apple-sub-1"
        });

        var result = await sut.LoginWithAppleAsync("valid-apple-token", null);

        Assert.True(result.IsSuccess);
        Assert.Equal(created.Id, result.Value.UserId);
        Assert.Single(users.Users);
    }

    [Fact]
    public async Task AppleLogin_DisabledUser_Fails()
    {
        var sut = CreateSut(out var users, out _, out _, out var apple, out var logins);
        var created = await users.CreateAsync("relay@privaterelay.appleid.com", "hash", "User", "Apple", null);
        users.Replace(new HotelResortBooking.Application.Abstractions.Repositories.UserAccountData
        {
            Id = created.Id,
            Email = created.Email,
            PasswordHash = created.PasswordHash,
            FirstName = created.FirstName,
            LastName = created.LastName,
            IsActive = false,
            Role = created.Role
        });
        await logins.AddAsync(created.Id, ExternalAuthProviders.Apple, "apple-sub-1");

        apple.NextResult = Result.Success(new ExternalIdentity
        {
            Provider = ExternalAuthProviders.Apple,
            ProviderUserId = "apple-sub-1"
        });

        var result = await sut.LoginWithAppleAsync("valid-apple-token", null);

        Assert.True(result.IsFailure);
        Assert.Equal("AccountDisabled", result.Error!.Code);
    }

    [Fact]
    public async Task DuplicateProviderIdentity_UsesExistingLink()
    {
        var sut = CreateSut(out var users, out _, out var google, out _, out var logins);
        var created = await users.CreateAsync("ada@gmail.com", "hash", "Ada", "Lovelace", null);
        await logins.AddAsync(created.Id, ExternalAuthProviders.Google, "google-sub-1");

        google.NextResult = Result.Success(new ExternalIdentity
        {
            Provider = ExternalAuthProviders.Google,
            ProviderUserId = "google-sub-1",
            Email = "other@gmail.com"
        });

        var first = await sut.LoginWithGoogleAsync("token-1", null);
        var second = await sut.LoginWithGoogleAsync("token-2", null);

        Assert.True(first.IsSuccess);
        Assert.True(second.IsSuccess);
        Assert.Equal(created.Id, first.Value.UserId);
        Assert.Equal(created.Id, second.Value.UserId);
        Assert.Single(users.Users);
        Assert.Single(logins.Logins);
    }

    [Fact]
    public async Task PasswordAccount_CanCoexistWithUnrelatedGoogleIdentity()
    {
        var sut = CreateSut(out var users, out _, out var google, out _, out var logins);
        await users.CreateAsync("password@example.com", "hash:Password1!", "Pat", "Word", null);

        google.NextResult = Result.Success(new ExternalIdentity
        {
            Provider = ExternalAuthProviders.Google,
            ProviderUserId = "google-sub-2",
            Email = "social@gmail.com",
            FirstName = "Social",
            LastName = "User"
        });

        var result = await sut.LoginWithGoogleAsync("valid-google-token", null);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, users.Users.Count);
        Assert.Single(logins.Logins);
        Assert.Equal("social@gmail.com", result.Value.Email);
    }

    [Fact]
    public async Task GoogleLogin_EmptyToken_FailsWithoutCallingCreate()
    {
        var sut = CreateSut(out var users, out _, out _, out _, out _);

        var result = await sut.LoginWithGoogleAsync("  ", null);

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidGoogleToken", result.Error!.Code);
        Assert.Empty(users.Users);
    }

    private static AuthenticationService CreateSut(
        out FakeUserRepository users,
        out FakeRefreshTokenRepository refreshTokens,
        out FakeGoogleIdTokenValidator google,
        out FakeAppleIdentityTokenValidator apple,
        out FakeExternalLoginRepository logins)
    {
        users = new FakeUserRepository();
        refreshTokens = new FakeRefreshTokenRepository();
        google = new FakeGoogleIdTokenValidator();
        apple = new FakeAppleIdentityTokenValidator();
        logins = new FakeExternalLoginRepository();
        var hasher = new FakePasswordHasher();
        var access = new FakeAccessContext();
        var clock = new FakeDateTimeProvider();
        var unitOfWork = new FakeUnitOfWork();
        var tokens = new FakeTokenService();

        var register = new RegisterUserUseCase(users, hasher, unitOfWork);
        var login = new LoginUserUseCase(users, hasher, clock, unitOfWork);
        var logout = new LogoutUserUseCase();
        var socialLogin = new SocialLoginUseCase(users, logins, hasher, clock, unitOfWork);

        return new AuthenticationService(
            register,
            login,
            logout,
            socialLogin,
            google,
            apple,
            users,
            refreshTokens,
            tokens,
            access,
            clock,
            unitOfWork,
            new FakeRolePermissionRepository());
    }
}
