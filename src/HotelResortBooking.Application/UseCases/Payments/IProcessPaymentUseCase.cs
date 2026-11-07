using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Payments;

public interface IProcessPaymentUseCase
{
    Task<Result<ProcessPaymentResponse>> ExecuteAsync(
        ProcessPaymentRequest request,
        CancellationToken cancellationToken = default);
}
