using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Bookings;

public interface ICreateBookingDraftUseCase
{
    Task<Result<CreateBookingDraftResponse>> ExecuteAsync(
        CreateBookingDraftRequest request,
        CancellationToken cancellationToken = default);
}
