using HotelResortBooking.Application.Features.Management.Rooms;

namespace HotelResortBooking.Api.Contracts.Rooms;

public sealed class RoomImageForm
{
    public IFormFile? File { get; set; }

    public string? AltText { get; set; }

    public int? SortOrder { get; set; }

    public bool IsPrimary { get; set; }

    public AddRoomImageRequest ToUseCaseRequest() => new()
    {
        FileContent = File?.OpenReadStream(),
        FileName = File?.FileName,
        AltText = AltText,
        SortOrder = SortOrder,
        IsPrimary = IsPrimary
    };
}
