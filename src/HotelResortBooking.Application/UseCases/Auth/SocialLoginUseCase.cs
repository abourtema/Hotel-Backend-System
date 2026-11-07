using System.Security.Cryptography;
using System.Text;
using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Application.Security;

namespace HotelResortBooking.Application.UseCases.Auth;

public sealed class SocialLoginRequest
{
    public string Provider { get; init; } = string.Empty;

    public string ProviderUserId { get; init; } = string.Empty;

    public string? Email { get; init; }

    public bool EmailVerified { get; init; }

    public string? FirstName { get; init; }

    public string? LastName { get; init; }
}

public sealed class SocialLoginResponse
{
    public Guid UserId { get; init; }
}

public interface ISocialLoginUseCase
{
    Task<Result<SocialLoginResponse>> ExecuteAsync(
        SocialLoginRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class SocialLoginUseCase : ISocialLoginUseCase
{
    private readonly IUserRepository _users;
    private readonly IExternalLoginRepository _externalLogins;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _unitOfWork;

    public SocialLoginUseCase(
        IUserRepository users,
        IExternalLoginRepository externalLogins,
        IPasswordHasher passwordHasher,
        IDateTimeProvider clock,
        IUnitOfWork unitOfWork)
    {
        _users = users;
        _externalLogins = externalLogins;
        _passwordHasher = passwordHasher;
        _clock = clock;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SocialLoginResponse>> ExecuteAsync(
        SocialLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Provider) || string.IsNullOrWhiteSpace(request.ProviderUserId))
        {
            return Result.Failure<SocialLoginResponse>(InvalidProviderToken(request.Provider));
        }

        var provider = request.Provider.Trim();
        var providerUserId = request.ProviderUserId.Trim();

        var existingLink = await _externalLogins.GetByProviderAsync(provider, providerUserId, cancellationToken);
        if (existingLink is not null)
        {
            var linkedUser = await _users.GetByIdAsync(existingLink.UserId, cancellationToken);
            if (linkedUser is null)
            {
                return Result.Failure<SocialLoginResponse>(BusinessErrors.UserNotFound());
            }

            if (!linkedUser.IsActive)
            {
                return Result.Failure<SocialLoginResponse>(BusinessErrors.AccountDisabled());
            }

            await _users.UpdateLastLoginAsync(linkedUser.Id, _clock.UtcNow, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(new SocialLoginResponse { UserId = linkedUser.Id });
        }

        var providerEmail = NormalizeEmail(request.Email);
        if (!string.IsNullOrWhiteSpace(providerEmail) &&
            await _users.EmailExistsAsync(providerEmail, cancellationToken))
        {
            return Result.Failure<SocialLoginResponse>(BusinessErrors.AccountConflict());
        }

        var email = string.IsNullOrWhiteSpace(providerEmail)
            ? CreatePlaceholderEmail(provider, providerUserId)
            : providerEmail;

        if (await _users.EmailExistsAsync(email, cancellationToken))
        {
            return Result.Failure<SocialLoginResponse>(BusinessErrors.AccountConflict());
        }

        var firstName = TrimOrFallback(request.FirstName, "User");
        var lastName = TrimOrFallback(request.LastName, provider);

        var user = await _users.CreateAsync(
            email,
            CreateUnusablePasswordHash(),
            firstName,
            lastName,
            phoneNumber: null,
            cancellationToken);

        await _externalLogins.AddAsync(user.Id, provider, providerUserId, cancellationToken);
        await _users.UpdateLastLoginAsync(user.Id, _clock.UtcNow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new SocialLoginResponse { UserId = user.Id });
    }

    private string CreateUnusablePasswordHash()
    {
        var secret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        return _passwordHasher.Hash(secret);
    }

    private static string CreatePlaceholderEmail(string provider, string providerUserId)
    {
        var providerPart = new string(provider.ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrWhiteSpace(providerPart))
        {
            providerPart = "social";
        }

        var subjectBytes = SHA256.HashData(Encoding.UTF8.GetBytes(providerUserId));
        var subjectPart = Convert.ToHexString(subjectBytes.AsSpan(0, 16)).ToLowerInvariant();
        return $"{providerPart}.{subjectPart}@users.noreply.local";
    }

    private static string? NormalizeEmail(string? email)
    {
        var trimmed = email?.Trim();
        if (string.IsNullOrWhiteSpace(trimmed) || !trimmed.Contains('@') || !trimmed.Contains('.'))
        {
            return null;
        }

        return trimmed.ToLowerInvariant();
    }

    private static string TrimOrFallback(string? value, string fallback)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? fallback : trimmed;
    }

    private static Error InvalidProviderToken(string provider)
    {
        if (string.Equals(provider, ExternalAuthProviders.Apple, StringComparison.OrdinalIgnoreCase))
        {
            return BusinessErrors.InvalidAppleToken();
        }

        return BusinessErrors.InvalidGoogleToken();
    }
}
