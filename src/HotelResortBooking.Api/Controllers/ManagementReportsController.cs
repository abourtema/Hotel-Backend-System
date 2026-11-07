using HotelResortBooking.Application.Features.Management.Reports;
using HotelResortBooking.Application.Security;
using HotelResortBooking.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelResortBooking.Api.Controllers;

[Authorize]
[Route("api/v1/management/reports")]
public sealed class ManagementReportsController : ApiControllerBase
{
    private readonly IManagementReportQueries _reports;

    public ManagementReportsController(IManagementReportQueries reports) => _reports = reports;

    [HttpGet("bookings")]
    [Authorize(Policy = Permissions.ReportsView)]
    public async Task<IActionResult> Bookings(
        [FromQuery] DateTimeOffset? dateFrom,
        [FromQuery] DateTimeOffset? dateTo,
        [FromQuery] Guid? propertyId,
        [FromQuery] BookingStatus? status,
        CancellationToken cancellationToken)
        => HandleResult(await _reports.GetBookingsAsync(new ReportFilter
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            PropertyId = propertyId,
            BookingStatus = status
        }, cancellationToken));

    [HttpGet("revenue")]
    [Authorize(Policy = Permissions.ReportsView)]
    public async Task<IActionResult> Revenue(
        [FromQuery] DateTimeOffset? dateFrom,
        [FromQuery] DateTimeOffset? dateTo,
        [FromQuery] Guid? propertyId,
        CancellationToken cancellationToken)
        => HandleResult(await _reports.GetRevenueAsync(new ReportFilter
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            PropertyId = propertyId
        }, cancellationToken));

    [HttpGet("occupancy")]
    [Authorize(Policy = Permissions.ReportsView)]
    public async Task<IActionResult> Occupancy(
        [FromQuery] DateTimeOffset? dateFrom,
        [FromQuery] DateTimeOffset? dateTo,
        [FromQuery] Guid? propertyId,
        CancellationToken cancellationToken)
        => HandleResult(await _reports.GetOccupancyAsync(new ReportFilter
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            PropertyId = propertyId
        }, cancellationToken));

    [HttpGet("payments")]
    [Authorize(Policy = Permissions.ReportsView)]
    public async Task<IActionResult> Payments(
        [FromQuery] DateTimeOffset? dateFrom,
        [FromQuery] DateTimeOffset? dateTo,
        [FromQuery] Guid? propertyId,
        [FromQuery] PaymentStatus? status,
        CancellationToken cancellationToken)
        => HandleResult(await _reports.GetPaymentsAsync(new ReportFilter
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            PropertyId = propertyId,
            PaymentStatus = status
        }, cancellationToken));
}
