using HotelResortBooking.Application.Abstractions.Repositories;

namespace HotelResortBooking.Application.UseCases.Bookings;

public sealed class GetUserBookingsResponse
{
    public IReadOnlyList<BookingDto> Upcoming { get; init; } = Array.Empty<BookingDto>();

    public IReadOnlyList<BookingDto> Ongoing { get; init; } = Array.Empty<BookingDto>();

    public IReadOnlyList<BookingDto> Past { get; init; } = Array.Empty<BookingDto>();

    public BookingListGroup? FilteredGroup { get; init; }
}
