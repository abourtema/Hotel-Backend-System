using System.ComponentModel.DataAnnotations;

namespace HotelResortBooking.Api.Contracts.Auth;

public sealed class GoogleLoginRequest
{
    [Required]
    public string IdToken { get; init; } = string.Empty;
}

public sealed class AppleLoginRequest
{
    [Required]
    public string IdentityToken { get; init; } = string.Empty;
}
