using HotelResortBooking.Application.Features.Management.Amenities;
using HotelResortBooking.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelResortBooking.Api.Controllers;

[Authorize]
[Route("api/v1/management/amenities")]
public sealed class ManagementAmenitiesController : ApiControllerBase
{
    private readonly IAmenityManagementCommands _amenities;

    public ManagementAmenitiesController(IAmenityManagementCommands amenities) => _amenities = amenities;

    [HttpGet]
    [Authorize(Policy = Permissions.AmenitiesView)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
        => HandleResult(await _amenities.ListAsync(cancellationToken));

    [HttpPost]
    [Authorize(Policy = Permissions.AmenitiesCreate)]
    public async Task<IActionResult> Create([FromBody] SaveAmenityRequest request, CancellationToken cancellationToken)
        => HandleResult(await _amenities.CreateAsync(request, cancellationToken));

    [HttpPut("{id:guid}")]
    [Authorize(Policy = Permissions.AmenitiesUpdate)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SaveAmenityRequest request, CancellationToken cancellationToken)
        => HandleResult(await _amenities.UpdateAsync(id, request, cancellationToken));

    [HttpPost("{id:guid}/activate")]
    [Authorize(Policy = Permissions.AmenitiesUpdate)]
    public async Task<IActionResult> Activate(Guid id, CancellationToken cancellationToken)
        => HandleResult(await _amenities.SetActiveAsync(id, true, cancellationToken));

    [HttpPost("{id:guid}/deactivate")]
    [Authorize(Policy = Permissions.AmenitiesDelete)]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken cancellationToken)
        => HandleResult(await _amenities.SetActiveAsync(id, false, cancellationToken));
}
