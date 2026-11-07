using HotelResortBooking.Application.Features.Management.Dashboard;
using HotelResortBooking.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelResortBooking.Api.Controllers;

[Authorize]
[Route("api/v1/management/dashboard")]
public sealed class ManagementDashboardController : ApiControllerBase
{
    private readonly IGetManagementDashboardQuery _dashboard;

    public ManagementDashboardController(IGetManagementDashboardQuery dashboard) => _dashboard = dashboard;

    [HttpGet]
    [Authorize(Policy = Permissions.ReportsView)]
    [ProducesResponseType(typeof(ManagementDashboardResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(
        [FromQuery] DateTimeOffset? dateFrom,
        [FromQuery] DateTimeOffset? dateTo,
        CancellationToken cancellationToken)
    {
        var result = await _dashboard.ExecuteAsync(
            new GetManagementDashboardQuery { DateFrom = dateFrom, DateTo = dateTo },
            cancellationToken);
        return Ok(result);
    }
}
