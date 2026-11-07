using HotelResortBooking.Application.Features.Management.Reviews;
using HotelResortBooking.Application.Security;
using HotelResortBooking.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelResortBooking.Api.Controllers;

[Authorize]
[Route("api/v1/management/reviews")]
public sealed class ManagementReviewsController : ApiControllerBase
{
    private readonly IReviewManagementCommands _reviews;

    public ManagementReviewsController(IReviewManagementCommands reviews) => _reviews = reviews;

    [HttpGet]
    [Authorize(Policy = Permissions.ReviewsView)]
    public async Task<IActionResult> List(
        [FromQuery] ReviewStatus? status,
        [FromQuery] Guid? propertyId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
        => HandleResult(await _reviews.ListAsync(new GetManagedReviewsQuery
        {
            Status = status,
            PropertyId = propertyId,
            Page = page,
            PageSize = pageSize
        }, cancellationToken));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = Permissions.ReviewsView)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
        => HandleResult(await _reviews.GetAsync(id, cancellationToken));

    [HttpPost("{id:guid}/approve")]
    [Authorize(Policy = Permissions.ReviewsModerate)]
    public async Task<IActionResult> Approve(Guid id, CancellationToken cancellationToken)
        => HandleResult(await _reviews.ModerateAsync(id, ReviewStatus.Published, cancellationToken));

    [HttpPost("{id:guid}/reject")]
    [Authorize(Policy = Permissions.ReviewsModerate)]
    public async Task<IActionResult> Reject(Guid id, CancellationToken cancellationToken)
        => HandleResult(await _reviews.ModerateAsync(id, ReviewStatus.Rejected, cancellationToken));

    [HttpPost("{id:guid}/hide")]
    [Authorize(Policy = Permissions.ReviewsModerate)]
    public async Task<IActionResult> Hide(Guid id, CancellationToken cancellationToken)
        => HandleResult(await _reviews.ModerateAsync(id, ReviewStatus.Hidden, cancellationToken));
}
