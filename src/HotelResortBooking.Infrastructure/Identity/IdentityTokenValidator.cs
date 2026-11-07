using System.Security.Claims;
using HotelResortBooking.Application.Abstractions.Security;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace HotelResortBooking.Infrastructure.Identity;

internal static class IdentityTokenValidator
{
    public static Result<ExternalIdentity> Validate(
        string token,
        TokenValidationParameters parameters,
        string provider,
        Func<Error> invalidToken,
        Func<Error> expiredToken)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return Result.Failure<ExternalIdentity>(invalidToken());
        }

        var handler = new JsonWebTokenHandler();
        TokenValidationResult result;
        try
        {
            result = handler.ValidateTokenAsync(token, parameters).GetAwaiter().GetResult();
        }
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException or FormatException)
        {
            return MapFailure(ex, expiredToken, invalidToken);
        }

        if (!result.IsValid)
        {
            return MapFailure(result.Exception, expiredToken, invalidToken);
        }

        var claims = result.ClaimsIdentity;
        var subject = GetClaim(claims, JwtRegisteredClaimNames.Sub, ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(subject))
        {
            return Result.Failure<ExternalIdentity>(invalidToken());
        }

        var email = GetClaim(claims, JwtRegisteredClaimNames.Email, ClaimTypes.Email);
        var emailVerified = IsTruthy(GetClaim(claims, "email_verified"));

        return Result.Success(new ExternalIdentity
        {
            Provider = provider,
            ProviderUserId = subject,
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            EmailVerified = emailVerified,
            FirstName = GetClaim(claims, JwtRegisteredClaimNames.GivenName, ClaimTypes.GivenName, "given_name"),
            LastName = GetClaim(claims, JwtRegisteredClaimNames.FamilyName, ClaimTypes.Surname, "family_name")
        });
    }

    private static Result<ExternalIdentity> MapFailure(Exception? exception, Func<Error> expiredToken, Func<Error> invalidToken)
    {
        return exception switch
        {
            SecurityTokenExpiredException => Result.Failure<ExternalIdentity>(expiredToken()),
            SecurityTokenInvalidLifetimeException => Result.Failure<ExternalIdentity>(expiredToken()),
            SecurityTokenInvalidAudienceException => Result.Failure<ExternalIdentity>(BusinessErrors.InvalidAudience()),
            SecurityTokenInvalidIssuerException => Result.Failure<ExternalIdentity>(BusinessErrors.InvalidIssuer()),
            _ => Result.Failure<ExternalIdentity>(invalidToken())
        };
    }

    private static string? GetClaim(ClaimsIdentity identity, params string[] types)
    {
        foreach (var type in types)
        {
            var value = identity.FindFirst(type)?.Value;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }

    private static bool IsTruthy(string? value)
        => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";
}
