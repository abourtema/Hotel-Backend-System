using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Bookings;

public interface IGetUserBookingsUseCase
{
    Task<Result<GetUserBookingsResponse>> ExecuteAsync(
        GetUserBookingsRequest request,
        CancellationToken cancellationToken = default);
}
