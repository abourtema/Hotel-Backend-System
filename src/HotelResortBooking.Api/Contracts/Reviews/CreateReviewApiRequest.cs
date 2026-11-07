using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.UseCases.Reviews;

namespace HotelResortBooking.Api.Contracts.Reviews;

public sealed class CreateReviewApiRequest
{
    public Guid BookingId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public List<string> Tags { get; set; } = [];

    public List<IFormFile> Photos { get; set; } = [];

    public AddReviewRequest ToUseCaseRequest() => new()
    {
        BookingId = BookingId,
        Rating = Rating,
        Comment = Comment,
        Tags = Tags,
        Photos = Photos
            .Where(file => file.Length > 0)
            .Select(file => new UploadedImageFile
            {
                FileName = file.FileName,
                Content = file.OpenReadStream()
            })
            .ToList()
    };
}
