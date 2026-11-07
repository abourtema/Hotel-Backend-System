using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Paging;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.Domain.Management;

namespace HotelResortBooking.Application.Features.Management.Reviews;

public sealed class GetManagedReviewsQuery
{
    public ReviewStatus? Status { get; init; }

    public Guid? PropertyId { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

public interface IReviewManagementCommands
{
    Task<Result<PagedResult<ManagedReviewListItem>>> ListAsync(
        GetManagedReviewsQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<ManagedReviewListItem>> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result> ModerateAsync(Guid id, ReviewStatus status, CancellationToken cancellationToken = default);
}

public sealed class ReviewManagementHandlers : IReviewManagementCommands
{
    private readonly IManagementQueryRepository _queries;
    private readonly IAuditLogger _audit;
    private readonly IUnitOfWork _unitOfWork;

    public ReviewManagementHandlers(
        IManagementQueryRepository queries,
        IAuditLogger audit,
        IUnitOfWork unitOfWork)
    {
        _queries = queries;
        _audit = audit;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PagedResult<ManagedReviewListItem>>> ListAsync(
        GetManagedReviewsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page <= 0 || query.PageSize <= 0)
        {
            return Result.Failure<PagedResult<ManagedReviewListItem>>(BusinessErrors.InvalidPagination());
        }

        var page = await _queries.SearchReviewsAsync(
            query.Status,
            query.PropertyId,
            query.Page,
            Math.Min(query.PageSize, 100),
            cancellationToken);
        return Result.Success(page);
    }

    public async Task<Result<ManagedReviewListItem>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var review = await _queries.GetReviewAsync(id, cancellationToken);
        return review is null
            ? Result.Failure<ManagedReviewListItem>(BusinessErrors.ReviewNotFound(id))
            : Result.Success(review);
    }

    public async Task<Result> ModerateAsync(Guid id, ReviewStatus status, CancellationToken cancellationToken = default)
    {
        var existing = await _queries.GetReviewAsync(id, cancellationToken);
        if (existing is null)
        {
            return Result.Failure(BusinessErrors.ReviewNotFound(id));
        }

        await _queries.SetReviewStatusAsync(id, status, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var action = status switch
        {
            ReviewStatus.Published => AuditActions.ReviewApproved,
            ReviewStatus.Rejected => AuditActions.ReviewRejected,
            ReviewStatus.Hidden => AuditActions.ReviewHidden,
            _ => AuditActions.ReviewApproved
        };

        await _audit.LogAsync(new AuditLogEntry
        {
            Action = action,
            EntityType = "Review",
            EntityId = id.ToString(),
            OldValues = new { existing.Status },
            NewValues = new { Status = status }
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
