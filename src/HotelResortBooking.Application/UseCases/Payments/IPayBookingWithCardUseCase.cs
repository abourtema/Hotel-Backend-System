using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Payments;

public interface IPayBookingWithCardUseCase
{
    Task<Result<PayBookingWithCardResponse>> ExecuteAsync(
        PayBookingWithCardRequest request,
        CancellationToken cancellationToken = default);
}
