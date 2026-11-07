using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Payments;

public interface IHandlePaymentFailureUseCase
{
    Task<Result<HandlePaymentFailureResponse>> ExecuteAsync(
        HandlePaymentFailureRequest request,
        CancellationToken cancellationToken = default);
}
