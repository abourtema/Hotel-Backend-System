using HotelResortBooking.Application.Abstractions.Repositories;

namespace HotelResortBooking.Application.Abstractions;

public static class ImageUrlMapping
{
    public static ManagedHotelDetails WithPublicImageUrls(this ManagedHotelDetails hotel, IImageUrlResolver urls)
        => new()
        {
            Id = hotel.Id,
            Name = hotel.Name,
            Description = hotel.Description,
            PropertyType = hotel.PropertyType,
            StarRating = hotel.StarRating,
            AddressLine = hotel.AddressLine,
            City = hotel.City,
            Country = hotel.Country,
            Latitude = hotel.Latitude,
            Longitude = hotel.Longitude,
            CheckInTime = hotel.CheckInTime,
            CheckOutTime = hotel.CheckOutTime,
            CancellationPolicy = hotel.CancellationPolicy,
            DepositPolicy = hotel.DepositPolicy,
            IsActive = hotel.IsActive,
            Images = hotel.Images.Select(image => image.WithPublicUrl(urls)).ToList(),
            Amenities = hotel.Amenities
        };

    public static ManagedRoomDto WithPublicImageUrls(this ManagedRoomDto room, IImageUrlResolver urls)
        => new()
        {
            Id = room.Id,
            PropertyId = room.PropertyId,
            PropertyName = room.PropertyName,
            Name = room.Name,
            Description = room.Description,
            RoomSize = room.RoomSize,
            MaxAdults = room.MaxAdults,
            MaxChildren = room.MaxChildren,
            MaxGuests = room.MaxGuests,
            BedType = room.BedType,
            BasePricePerNight = room.BasePricePerNight,
            Currency = room.Currency,
            IsActive = room.IsActive,
            Images = room.Images.Select(image => image.WithPublicUrl(urls)).ToList()
        };

    public static ManagedImageDto WithPublicUrl(this ManagedImageDto image, IImageUrlResolver urls)
        => new()
        {
            Id = image.Id,
            ImageUrl = urls.ToPublicUrl(image.ImageUrl),
            AltText = image.AltText,
            SortOrder = image.SortOrder,
            IsPrimary = image.IsPrimary
        };
}
