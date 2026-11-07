using HotelResortBooking.Application.Features.Management.Upgrades;
using HotelResortBooking.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelResortBooking.Api.Controllers;

[Authorize]
[Route("api/v1/management/upgrades")]
public sealed class ManagementUpgradesController : ApiControllerBase
{
    private readonly IUpgradeManagementCommands _upgrades;

    public ManagementUpgradesController(IUpgradeManagementCommands upgrades) => _upgrades = upgrades;

    [HttpGet]
    [Authorize(Policy = Permissions.UpgradesView)]
    public async Task<IActionResult> List([FromQuery] Guid? propertyId, CancellationToken cancellationToken)
        => HandleResult(await _upgrades.ListAsync(propertyId, cancellationToken));

    [HttpPost]
    [Authorize(Policy = Permissions.UpgradesCreate)]
    public async Task<IActionResult> Create([FromBody] SaveUpgradeRequest request, CancellationToken cancellationToken)
        => HandleResult(await _upgrades.CreateAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.UpgradesUpdate)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveUpgradeRequest request, CancellationToken cancellationToken)
        => HandleResult(await _upgrades.UpdateAsync(id, request, cancellationToken));

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = Permissions.UpgradesDelete)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
        => HandleResult(await _upgrades.DeleteAsync(id, cancellationToken));
}
