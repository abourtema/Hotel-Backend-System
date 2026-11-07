using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Bookings;

public interface ICancelBookingUseCase
{
    Task<Result<CancelBookingResponse>> ExecuteAsync(
        CancelBookingRequest request,
        CancellationToken cancellationToken = default);
}
