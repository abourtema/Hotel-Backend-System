using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.UseCases.Reviews;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.UnitTests.Fakes;
using Xunit;

namespace HotelResortBooking.UnitTests.UseCases;

public sealed class AddReviewUseCaseTests
{
    private readonly FakeAccessContext _access = new();
    private readonly FakeBookingRepository _bookings = new();
    private readonly FakeReviewRepository _reviews = new();
    private readonly FakeImageStorage _storage = new();
    private readonly FakeImageUrlResolver _urls = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly AddReviewUseCase _sut;

    private readonly Guid _userId = Guid.NewGuid();

    public AddReviewUseCaseTests()
    {
        _sut = new AddReviewUseCase(_access, _bookings, _reviews, _storage, _urls, _uow);
    }

    [Fact]
    public async Task ValidReview_OnCompletedBooking_Succeeds()
    {
        _access.Authenticate(_userId);
        var booking = CreateBooking(BookingStatus.Completed);
        _bookings.Seed(booking);

        var result = await _sut.ExecuteAsync(new AddReviewRequest
        {
            BookingId = booking.Id,
            Rating = 5,
            Comment = "Great stay",
            Tags = ["clean"],
            Photos = []
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Rating);
        Assert.Equal(booking.Id, result.Value.BookingId);
        Assert.Equal(1, _uow.SaveChangesCallCount);
    }

    [Fact]
    public async Task ValidReview_WithPhoto_StoresRelativePath()
    {
        _access.Authenticate(_userId);
        var booking = CreateBooking(BookingStatus.Completed);
        _bookings.Seed(booking);
        await using var stream = new MemoryStream("photo"u8.ToArray());

        var result = await _sut.ExecuteAsync(new AddReviewRequest
        {
            BookingId = booking.Id,
            Rating = 5,
            Photos = [new UploadedImageFile { FileName = "stay.jpg", Content = stream }]
        });

        Assert.True(result.IsSuccess);
        Assert.Single(_storage.SavedPaths);
        Assert.StartsWith("/uploads/", result.Value.PhotoUrls[0]);
    }

    [Fact]
    public async Task ReviewWithoutCompletedBooking_FailsReviewNotAllowed()
    {
        _access.Authenticate(_userId);
        var booking = CreateBooking(BookingStatus.Confirmed);
        _bookings.Seed(booking);

        var result = await _sut.ExecuteAsync(new AddReviewRequest
        {
            BookingId = booking.Id,
            Rating = 4
        });

        Assert.True(result.IsFailure);
        Assert.Equal("ReviewNotAllowed", result.Error!.Code);
    }

    [Fact]
    public async Task DuplicateReview_Fails()
    {
        _access.Authenticate(_userId);
        var booking = CreateBooking(BookingStatus.Completed);
        _bookings.Seed(booking);
        _reviews.Reviews.Add(new ReviewData
        {
            Id = Guid.NewGuid(),
            UserId = _userId,
            PropertyId = booking.PropertyId,
            BookingId = booking.Id,
            Rating = 3,
            Status = ReviewStatus.Published
        });

        var result = await _sut.ExecuteAsync(new AddReviewRequest
        {
            BookingId = booking.Id,
            Rating = 5
        });

        Assert.True(result.IsFailure);
        Assert.Equal("ReviewAlreadyExists", result.Error!.Code);
    }

    [Fact]
    public async Task InvalidRating_Fails()
    {
        _access.Authenticate(_userId);
        var booking = CreateBooking(BookingStatus.Completed);
        _bookings.Seed(booking);

        var result = await _sut.ExecuteAsync(new AddReviewRequest
        {
            BookingId = booking.Id,
            Rating = 0
        });

        Assert.True(result.IsFailure);
        Assert.Equal("InvalidRating", result.Error!.Code);
    }

    private BookingData CreateBooking(BookingStatus status) => new()
    {
        Id = Guid.NewGuid(),
        BookingNumber = "BKREV1",
        UserId = _userId,
        PropertyId = Guid.NewGuid(),
        PropertyName = "Hotel",
        CheckInDate = new DateOnly(2026, 7, 1),
        CheckOutDate = new DateOnly(2026, 7, 3),
        Adults = 2,
        RoomsCount = 1,
        Status = status,
        Currency = "USD",
        TotalAmount = 200m,
        CreatedAt = DateTimeOffset.UtcNow
    };
}
