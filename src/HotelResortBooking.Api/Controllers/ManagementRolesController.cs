using HotelResortBooking.Application.Features.Management.Users;
using HotelResortBooking.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelResortBooking.Api.Controllers;

[Authorize]
[Route("api/v1/management/roles")]
public sealed class ManagementRolesController : ApiControllerBase
{
    private readonly IRoleManagementQueries _roles;

    public ManagementRolesController(IRoleManagementQueries roles) => _roles = roles;

    [HttpGet]
    [Authorize(Policy = Permissions.RolesView)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
        => HandleResult(await _roles.ListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.RolesView)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => HandleResult(await _roles.GetAsync(id, cancellationToken));
}
