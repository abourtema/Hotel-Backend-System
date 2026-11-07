using System.ComponentModel.DataAnnotations;

namespace HotelResortBooking.Api.Contracts.Auth;

public sealed class RevokeTokenRequest
{
    [Required]
    public string RefreshToken { get; init; } = string.Empty;
}
