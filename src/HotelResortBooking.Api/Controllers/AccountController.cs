using HotelResortBooking.Api.Contracts.Auth;
using HotelResortBooking.Application.Services;
using HotelResortBooking.Application.UseCases.Auth;
using HotelResortBooking.Application.UseCases.Loyalty;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelResortBooking.Api.Controllers;

[Authorize]
[Route("api/v1/account")]
public sealed class AccountController : ApiControllerBase
{
    private readonly IAuthenticationService _authService;
    private readonly IGetCurrentAccessStateUseCase _accessState;
    private readonly IGetLoyaltyBalanceUseCase _loyaltyBalance;

    public AccountController(
        IAuthenticationService authService,
        IGetCurrentAccessStateUseCase accessState,
        IGetLoyaltyBalanceUseCase loyaltyBalance)
    {
        _authService = authService;
        _accessState = accessState;
        _loyaltyBalance = loyaltyBalance;
    }

    [HttpGet]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
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

    [HttpGet("session")]
    [ProducesResponseType(typeof(GetCurrentAccessStateResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSession(CancellationToken cancellationToken)
    {
        return HandleResult(await _accessState.ExecuteAsync(cancellationToken));
    }

    [HttpGet("points")]
    [ProducesResponseType(typeof(GetLoyaltyBalanceResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPoints(CancellationToken cancellationToken)
    {
        return HandleResult(await _loyaltyBalance.ExecuteAsync(cancellationToken));
    }
}
