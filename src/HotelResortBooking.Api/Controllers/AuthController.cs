using HotelResortBooking.Api.Contracts.Auth;
using HotelResortBooking.Application.Services;
using HotelResortBooking.Application.UseCases.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelResortBooking.Api.Controllers;

[Route("api/v1/auth")]
public sealed class AuthController : ApiControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly IContinueAsGuestUseCase _continueAsGuest;

    public AuthController(
        IAuthenticationService authService,
        IContinueAsGuestUseCase continueAsGuest)
    {
        _authService = authService;
        _continueAsGuest = continueAsGuest;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(
            new RegisterUserRequest
            {
                Email = request.Email,
                Password = request.Password,
                FirstName = request.FirstName,
                LastName = request.LastName,
                PhoneNumber = request.PhoneNumber
            },
            GetClientIpAddress(),
            cancellationToken);

        return HandleResult(result, auth => StatusCode(StatusCodes.Status201Created, Map(auth)));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginAsync(
            new LoginUserRequest
            {
                Email = request.Email,
                Password = request.Password
            },
            GetClientIpAddress(),
            cancellationToken);

        return HandleResult(result, auth => Ok(Map(auth)));
    }

    /// <summary>
    /// Authenticates with a Google ID token issued to this application's configured client ID(s).
    /// Returns the same session payload as email/password login.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("google")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GoogleLogin(
        [FromBody] GoogleLoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginWithGoogleAsync(
            request.IdToken,
            GetClientIpAddress(),
            cancellationToken);

        return HandleResult(result, auth => Ok(Map(auth)));
    }

    /// <summary>
    /// Authenticates with an Apple identity token issued to this application's configured client ID(s).
    /// Returns the same session payload as email/password login.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("apple")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AppleLogin(
        [FromBody] AppleLoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LoginWithAppleAsync(
            request.IdentityToken,
            GetClientIpAddress(),
            cancellationToken);

        return HandleResult(result, auth => Ok(Map(auth)));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AuthenticationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshAsync(
            request.RefreshToken,
            GetClientIpAddress(),
            cancellationToken);

        return HandleResult(result, auth => Ok(Map(auth)));
    }

    [AllowAnonymous]
    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Revoke(
        [FromBody] RevokeTokenRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.RevokeAsync(
            request.RefreshToken,
            GetClientIpAddress(),
            cancellationToken);

        return HandleResult(result, () => NoContent());
    }

    [Authorize]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest? request,
        CancellationToken cancellationToken)
    {
        var result = await _authService.LogoutAsync(
            request?.RefreshToken ?? string.Empty,
            GetClientIpAddress(),
            cancellationToken);

        return HandleResult(result, () => Ok(new { message = "Logged out successfully." }));
    }

    [AllowAnonymous]
    [HttpPost("guest")]
    [ProducesResponseType(typeof(ContinueAsGuestResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ContinueAsGuest(CancellationToken cancellationToken)
    {
        var result = await _continueAsGuest.ExecuteAsync(new ContinueAsGuestRequest(), cancellationToken);
        return HandleResult(result);
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        var result = await _authService.GetCurrentUserAsync(cancellationToken);

        return HandleResult(result, user => Ok(new UserResponse
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role
        }));
    }

    private static AuthenticationResponse Map(AuthenticationResult auth) => new()
    {
        UserId = auth.UserId,
        Email = auth.Email,
        FirstName = auth.FirstName,
        LastName = auth.LastName,
        Role = auth.Role,
        AccessToken = auth.AccessToken,
        RefreshToken = auth.RefreshToken,
        AccessTokenExpiresAt = auth.AccessTokenExpiresAt,
        RefreshTokenExpiresAt = auth.RefreshTokenExpiresAt,
        TokenType = auth.TokenType
    };
}
