using HotelResortBooking.Application.Features.Management.Hotels;

namespace HotelResortBooking.Api.Contracts.Hotels;

public sealed class HotelImageForm
{
    public IFormFile? File { get; set; }

    public string? AltText { get; set; }

    public int? SortOrder { get; set; }

    public bool IsPrimary { get; set; }

    public AddHotelImageRequest ToUseCaseRequest() => new()
    {
        FileContent = File?.OpenReadStream(),
        FileName = File?.FileName,
        AltText = AltText,
        SortOrder = SortOrder,
        IsPrimary = IsPrimary
    };
}
