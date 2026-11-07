using HotelResortBooking.Application.Abstractions.Repositories;

namespace HotelResortBooking.Application.UseCases.Bookings;

public sealed class GetUserBookingsRequest
{
    public BookingListGroup? Group { get; init; }
}
