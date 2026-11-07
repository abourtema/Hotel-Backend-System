using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Bookings;

public interface ICreateBookingUseCase
{
    Task<Result<CreateBookingResponse>> ExecuteAsync(
        CreateBookingRequest request,
        CancellationToken cancellationToken = default);
}
