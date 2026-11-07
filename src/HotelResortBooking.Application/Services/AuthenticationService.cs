using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Abstractions.Security;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Application.Security;
using HotelResortBooking.Application.UseCases.Auth;

namespace HotelResortBooking.Application.Services;

public sealed class AuthenticationResult
{
    public Guid UserId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Role { get; init; } = Roles.User;

    public string AccessToken { get; init; } = string.Empty;

    public string RefreshToken { get; init; } = string.Empty;

    public DateTimeOffset AccessTokenExpiresAt { get; init; }

    public DateTimeOffset RefreshTokenExpiresAt { get; init; }

    public string TokenType { get; init; } = "Bearer";
}

public sealed class CurrentUserResult
{
    public Guid UserId { get; init; }

    public string Email { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string? PhoneNumber { get; init; }

    public string Role { get; init; } = Roles.User;

    public AccessMode AccessMode { get; init; }
}

public interface IAuthenticationService
{
    Task<Result<AuthenticationResult>> RegisterAsync(
        RegisterUserRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<Result<AuthenticationResult>> LoginAsync(
        LoginUserRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<Result<AuthenticationResult>> LoginWithGoogleAsync(
        string idToken,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<Result<AuthenticationResult>> LoginWithAppleAsync(
        string identityToken,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<Result<AuthenticationResult>> RefreshAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<Result> LogoutAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<Result> RevokeAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default);

    Task<Result<CurrentUserResult>> GetCurrentUserAsync(CancellationToken cancellationToken = default);
}

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IRegisterUserUseCase _registerUser;
    private readonly ILoginUserUseCase _loginUser;
    private readonly ILogoutUserUseCase _logoutUser;
    private readonly ISocialLoginUseCase _socialLogin;
    private readonly IGoogleIdTokenValidator _googleTokens;
    private readonly IAppleIdentityTokenValidator _appleTokens;
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ITokenService _tokens;
    private readonly IAccessContext _accessContext;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRolePermissionRepository _roles;

    public AuthenticationService(
        IRegisterUserUseCase registerUser,
        ILoginUserUseCase loginUser,
        ILogoutUserUseCase logoutUser,
        ISocialLoginUseCase socialLogin,
        IGoogleIdTokenValidator googleTokens,
        IAppleIdentityTokenValidator appleTokens,
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        ITokenService tokens,
        IAccessContext accessContext,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork,
        IRolePermissionRepository roles)
    {
        _registerUser = registerUser;
        _loginUser = loginUser;
        _logoutUser = logoutUser;
        _socialLogin = socialLogin;
        _googleTokens = googleTokens;
        _appleTokens = appleTokens;
        _users = users;
        _refreshTokens = refreshTokens;
        _tokens = tokens;
        _accessContext = accessContext;
        _clock = clock;
        _unitOfWork = unitOfWork;
        _roles = roles;
    }

    public async Task<Result<AuthenticationResult>> RegisterAsync(
        RegisterUserRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateRegistration(request);
        if (validationError is not null)
        {
            return Result.Failure<AuthenticationResult>(validationError);
        }

        var registered = await _registerUser.ExecuteAsync(request, cancellationToken);
        if (registered.IsFailure)
        {
            return Result.Failure<AuthenticationResult>(registered.Error!);
        }

        var user = await _users.GetByIdAsync(registered.Value.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<AuthenticationResult>(BusinessErrors.UserNotFound());
        }

        return Result.Success(await IssueSessionAsync(user, ipAddress, cancellationToken));
    }

    public async Task<Result<AuthenticationResult>> LoginAsync(
        LoginUserRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var loggedIn = await _loginUser.ExecuteAsync(request, cancellationToken);
        if (loggedIn.IsFailure)
        {
            return Result.Failure<AuthenticationResult>(loggedIn.Error!);
        }

        var user = await _users.GetByIdAsync(loggedIn.Value.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result.Failure<AuthenticationResult>(BusinessErrors.InvalidCredentials());
        }

        return Result.Success(await IssueSessionAsync(user, ipAddress, cancellationToken));
    }

    public async Task<Result<AuthenticationResult>> LoginWithGoogleAsync(
        string idToken,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            return Result.Failure<AuthenticationResult>(BusinessErrors.InvalidGoogleToken());
        }

        var identity = await _googleTokens.ValidateAsync(idToken, cancellationToken);
        if (identity.IsFailure)
        {
            return Result.Failure<AuthenticationResult>(identity.Error!);
        }

        return await CompleteSocialLoginAsync(identity.Value, ipAddress, cancellationToken);
    }

    public async Task<Result<AuthenticationResult>> LoginWithAppleAsync(
        string identityToken,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identityToken))
        {
            return Result.Failure<AuthenticationResult>(BusinessErrors.InvalidAppleToken());
        }

        var identity = await _appleTokens.ValidateAsync(identityToken, cancellationToken);
        if (identity.IsFailure)
        {
            return Result.Failure<AuthenticationResult>(identity.Error!);
        }

        return await CompleteSocialLoginAsync(identity.Value, ipAddress, cancellationToken);
    }

    public async Task<Result<AuthenticationResult>> RefreshAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result.Failure<AuthenticationResult>(BusinessErrors.InvalidRefreshToken());
        }

        var stored = await _refreshTokens.GetByHashAsync(_tokens.HashToken(refreshToken), cancellationToken);
        if (stored is null)
        {
            return Result.Failure<AuthenticationResult>(BusinessErrors.InvalidRefreshToken());
        }

        var utcNow = _clock.UtcNow;

        if (stored.IsRevoked)
        {
            await _refreshTokens.RevokeAllForUserAsync(stored.UserId, utcNow, ipAddress, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure<AuthenticationResult>(BusinessErrors.RevokedRefreshToken());
        }

        if (utcNow >= stored.ExpiresAt)
        {
            return Result.Failure<AuthenticationResult>(BusinessErrors.ExpiredRefreshToken());
        }

        var user = await _users.GetByIdAsync(stored.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return Result.Failure<AuthenticationResult>(BusinessErrors.InvalidRefreshToken());
        }

        var replacement = _tokens.CreateRefreshToken();
        var replacementRecord = new RefreshTokenData
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = replacement.Hash,
            ExpiresAt = replacement.ExpiresAt,
            CreatedAt = utcNow,
            CreatedByIp = ipAddress
        };

        stored.RevokedAt = utcNow;
        stored.RevokedByIp = ipAddress;
        stored.ReplacedByTokenId = replacementRecord.Id;

        await _refreshTokens.UpdateAsync(stored, cancellationToken);
        await _refreshTokens.AddAsync(replacementRecord, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var access = await CreateAccessTokenAsync(user, cancellationToken);
        return Result.Success(Map(user, access, replacement.RawToken, replacement.ExpiresAt));
    }

    public async Task<Result> RevokeAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result.Failure(BusinessErrors.InvalidRefreshToken());
        }

        var stored = await _refreshTokens.GetByHashAsync(_tokens.HashToken(refreshToken), cancellationToken);
        if (stored is null)
        {
            return Result.Failure(BusinessErrors.InvalidRefreshToken());
        }

        if (stored.IsRevoked)
        {
            await _refreshTokens.RevokeAllForUserAsync(stored.UserId, _clock.UtcNow, ipAddress, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Failure(BusinessErrors.RevokedRefreshToken());
        }

        stored.RevokedAt = _clock.UtcNow;
        stored.RevokedByIp = ipAddress;
        await _refreshTokens.UpdateAsync(stored, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result> LogoutAsync(
        string refreshToken,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (!_accessContext.IsAuthenticated || _accessContext.UserId is null)
        {
            return Result.Failure(BusinessErrors.AuthenticationRequired());
        }

        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var stored = await _refreshTokens.GetByHashAsync(_tokens.HashToken(refreshToken), cancellationToken);
            if (stored is not null && stored.UserId == _accessContext.UserId.Value && !stored.IsRevoked)
            {
                stored.RevokedAt = _clock.UtcNow;
                stored.RevokedByIp = ipAddress;
                await _refreshTokens.UpdateAsync(stored, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        var logout = await _logoutUser.ExecuteAsync(cancellationToken);
        return logout.IsFailure ? Result.Failure(logout.Error!) : Result.Success();
    }

    public async Task<Result<CurrentUserResult>> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        if (!_accessContext.IsAuthenticated || _accessContext.UserId is null)
        {
            return Result.Failure<CurrentUserResult>(BusinessErrors.AuthenticationRequired());
        }

        var user = await _users.GetByIdAsync(_accessContext.UserId.Value, cancellationToken);
        if (user is null)
        {
            return Result.Failure<CurrentUserResult>(BusinessErrors.UserNotFound());
        }

        return Result.Success(new CurrentUserResult
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role,
            AccessMode = AccessMode.Authenticated
        });
    }

    private async Task<Result<AuthenticationResult>> CompleteSocialLoginAsync(
        ExternalIdentity identity,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var social = await _socialLogin.ExecuteAsync(
            new SocialLoginRequest
            {
                Provider = identity.Provider,
                ProviderUserId = identity.ProviderUserId,
                Email = identity.Email,
                EmailVerified = identity.EmailVerified,
                FirstName = identity.FirstName,
                LastName = identity.LastName
            },
            cancellationToken);

        if (social.IsFailure)
        {
            return Result.Failure<AuthenticationResult>(social.Error!);
        }

        var user = await _users.GetByIdAsync(social.Value.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<AuthenticationResult>(BusinessErrors.UserNotFound());
        }

        if (!user.IsActive)
        {
            return Result.Failure<AuthenticationResult>(BusinessErrors.AccountDisabled());
        }

        return Result.Success(await IssueSessionAsync(user, ipAddress, cancellationToken));
    }

    private async Task<AuthenticationResult> IssueSessionAsync(
        UserAccountData user,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        var access = await CreateAccessTokenAsync(user, cancellationToken);
        var refresh = _tokens.CreateRefreshToken();

        await _refreshTokens.AddAsync(
            new RefreshTokenData
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = refresh.Hash,
                ExpiresAt = refresh.ExpiresAt,
                CreatedAt = _clock.UtcNow,
                CreatedByIp = ipAddress
            },
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(user, access, refresh.RawToken, refresh.ExpiresAt);
    }

    private async Task<AccessTokenResult> CreateAccessTokenAsync(
        UserAccountData user,
        CancellationToken cancellationToken)
    {
        var roles = await _roles.GetRoleNamesForUserAsync(user.Id, cancellationToken);
        var permissions = await _roles.GetPermissionNamesForUserAsync(user.Id, cancellationToken);
        return _tokens.CreateAccessToken(user.Id, user.Email, user.Role, roles, permissions);
    }

    private static AuthenticationResult Map(
        UserAccountData user,
        AccessTokenResult access,
        string refreshToken,
        DateTimeOffset refreshExpiresAt)
        => new()
        {
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = string.IsNullOrWhiteSpace(user.Role) ? Roles.User : user.Role,
            AccessToken = access.Token,
            RefreshToken = refreshToken,
            AccessTokenExpiresAt = access.ExpiresAt,
            RefreshTokenExpiresAt = refreshExpiresAt
        };

    private static Error? ValidateRegistration(RegisterUserRequest request)
    {
        var email = request.Email?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@') || !email.Contains('.'))
        {
            return BusinessErrors.InvalidEmail();
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return BusinessErrors.WeakPassword();
        }

        return null;
    }
}
