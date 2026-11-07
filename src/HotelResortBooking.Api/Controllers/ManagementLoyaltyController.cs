using HotelResortBooking.Application.Features.Management.Loyalty;
using HotelResortBooking.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelResortBooking.Api.Controllers;

[Authorize]
[Route("api/v1/management/loyalty")]
public sealed class ManagementLoyaltyController : ApiControllerBase
{
    private readonly ILoyaltyManagementCommands _loyalty;

    public ManagementLoyaltyController(ILoyaltyManagementCommands loyalty) => _loyalty = loyalty;

    [HttpGet("accounts/{userId:guid}")]
    [Authorize(Policy = Permissions.LoyaltyView)]
    public async Task<IActionResult> GetAccount(Guid userId, CancellationToken cancellationToken)
        => HandleResult(await _loyalty.GetAccountAsync(userId, cancellationToken));

    [HttpGet("accounts/{userId:guid}/transactions")]
    [Authorize(Policy = Permissions.LoyaltyView)]
    public async Task<IActionResult> GetTransactions(Guid userId, CancellationToken cancellationToken)
        => HandleResult(await _loyalty.GetTransactionsAsync(userId, cancellationToken));

    [HttpPost("accounts/{userId:guid}/adjust")]
    [Authorize(Policy = Permissions.LoyaltyAdjust)]
    public async Task<IActionResult> Adjust(Guid userId, [FromBody] AdjustLoyaltyRequest request, CancellationToken cancellationToken)
        => HandleResult(await _loyalty.AdjustAsync(userId, request, cancellationToken));
}
