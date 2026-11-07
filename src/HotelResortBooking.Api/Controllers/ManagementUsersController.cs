using HotelResortBooking.Application.Features.Management.Users;
using HotelResortBooking.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelResortBooking.Api.Controllers;

[Authorize]
[Route("api/v1/management/users")]
public sealed class ManagementUsersController : ApiControllerBase
{
    private readonly IManagementUserCommands _users;

    public ManagementUsersController(IManagementUserCommands users) => _users = users;

    [HttpGet]
    [Authorize(Policy = Permissions.UsersView)]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => HandleResult(await _users.ListAsync(search, page, pageSize, cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.UsersView)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => HandleResult(await _users.GetAsync(id, cancellationToken));

    [HttpPost]
    [Authorize(Policy = Permissions.UsersCreate)]
    public async Task<IActionResult> Create([FromBody] CreateManagementUserRequest request, CancellationToken cancellationToken)
        => HandleResult(await _users.CreateAsync(request, cancellationToken), created => CreatedAtAction(nameof(Get), new { id = created.Id }, created));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.UsersUpdate)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateManagementUserRequest request, CancellationToken cancellationToken)
        => HandleResult(await _users.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = Permissions.UsersManage)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
        => HandleResult(await _users.SetActiveAsync(id, true, cancellationToken));

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = Permissions.UsersManage)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
        => HandleResult(await _users.SetActiveAsync(id, false, cancellationToken));

    [HttpPost("{id:guid}/roles")]
    [Authorize(Policy = Permissions.RolesManage)]
    public async Task<IActionResult> AssignRole(Guid id, [FromBody] AssignRoleRequest request, CancellationToken cancellationToken)
        => HandleResult(await _users.AssignRoleAsync(id, request.RoleId, cancellationToken));

    [HttpDelete("{id:guid}/roles/{roleId:guid}")]
    [Authorize(Policy = Permissions.RolesManage)]
    public async Task<IActionResult> RemoveRole(Guid id, Guid roleId, CancellationToken cancellationToken)
        => HandleResult(await _users.RemoveRoleAsync(id, roleId, cancellationToken));
}
