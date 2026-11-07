using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Bookings;

public interface IModifyBookingUseCase
{
    Task<Result<ModifyBookingResponse>> ExecuteAsync(
        ModifyBookingRequest request,
        CancellationToken cancellationToken = default);
}
