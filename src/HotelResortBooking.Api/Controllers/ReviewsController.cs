using HotelResortBooking.Api.Contracts.Reviews;
using HotelResortBooking.Application.UseCases.Reviews;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HotelResortBooking.Api.Controllers;

[Authorize]
[Route("api/v1/reviews")]
public sealed class ReviewsController : ApiControllerBase
{
    private readonly IAddReviewUseCase _addReview;

    public ReviewsController(IAddReviewUseCase addReview)
    {
        _addReview = addReview;
    }

    [HttpPost]
    [ProducesResponseType(typeof(AddReviewResponse), StatusCodes.Status201Created)]
    [RequestSizeLimit(20_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 20_000_000)]
    public async Task<IActionResult> Create(
        [FromForm] CreateReviewApiRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _addReview.ExecuteAsync(request.ToUseCaseRequest(), cancellationToken);
        return HandleResult(result, created => StatusCode(StatusCodes.Status201Created, created));
    }
}
