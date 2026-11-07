using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Bookings;

public interface IGetBookingUseCase
{
    Task<Result<GetBookingResponse>> ExecuteAsync(
        GetBookingRequest request,
        CancellationToken cancellationToken = default);
}
