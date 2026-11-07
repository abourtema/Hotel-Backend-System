using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.Domain.Rules;

namespace HotelResortBooking.Application.UseCases.Reviews;

public sealed class AddReviewUseCase : IAddReviewUseCase
{
    private readonly IAccessContext _accessContext;
    private readonly IBookingRepository _bookingRepository;
    private readonly IReviewRepository _reviewRepository;
    private readonly IImageStorage _storage;
    private readonly IImageUrlResolver _urls;
    private readonly IUnitOfWork _unitOfWork;

    public AddReviewUseCase(
        IAccessContext accessContext,
        IBookingRepository bookingRepository,
        IReviewRepository reviewRepository,
        IImageStorage storage,
        IImageUrlResolver urls,
        IUnitOfWork unitOfWork)
    {
        _accessContext = accessContext;
        _bookingRepository = bookingRepository;
        _reviewRepository = reviewRepository;
        _storage = storage;
        _urls = urls;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<AddReviewResponse>> ExecuteAsync(
        AddReviewRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_accessContext.IsAuthenticated || _accessContext.UserId is null)
        {
            return Result.Failure<AddReviewResponse>(BusinessErrors.AuthenticationRequired());
        }

        if (new RatingMustBeValidRule(request.Rating).IsBroken())
        {
            return Result.Failure<AddReviewResponse>(BusinessErrors.InvalidRating());
        }

        var booking = await _bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);
        if (booking is null)
        {
            return Result.Failure<AddReviewResponse>(BusinessErrors.BookingNotFound(request.BookingId));
        }

        if (booking.UserId != _accessContext.UserId)
        {
            return Result.Failure<AddReviewResponse>(BusinessErrors.UnauthorizedBookingAccess());
        }

        if (booking.Status != BookingStatus.Completed)
        {
            return Result.Failure<AddReviewResponse>(BusinessErrors.ReviewNotAllowed());
        }

        var userId = _accessContext.UserId.Value;
        if (await _reviewRepository.ExistsForBookingAsync(request.BookingId, userId, cancellationToken))
        {
            return Result.Failure<AddReviewResponse>(BusinessErrors.ReviewAlreadyExists());
        }

        var photoPaths = new List<string>();
        foreach (var photo in request.Photos)
        {
            var saved = await _storage.SaveAsync(
                ImageStorageCategories.Reviews,
                request.BookingId,
                photo.FileName,
                photo.Content,
                cancellationToken);
            if (saved.IsFailure)
            {
                await DeleteSavedAsync(photoPaths, cancellationToken);
                return Result.Failure<AddReviewResponse>(saved.Error!);
            }

            photoPaths.Add(saved.Value);
        }

        var review = await _reviewRepository.AddAsync(
            userId,
            booking.PropertyId,
            booking.Id,
            request.Rating,
            request.Comment,
            request.Tags,
            photoPaths,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new AddReviewResponse
        {
            ReviewId = review.Id,
            BookingId = review.BookingId,
            PropertyId = review.PropertyId,
            Rating = review.Rating,
            Comment = review.Comment,
            Status = review.Status,
            Tags = review.Tags,
            PhotoUrls = review.PhotoUrls.Select(_urls.ToPublicUrl).ToList()
        });
    }

    private async Task DeleteSavedAsync(IEnumerable<string> paths, CancellationToken cancellationToken)
    {
        foreach (var path in paths)
        {
            await _storage.DeleteAsync(path, cancellationToken);
        }
    }
}
