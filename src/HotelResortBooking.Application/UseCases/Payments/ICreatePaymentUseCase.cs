using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Payments;

public interface ICreatePaymentUseCase
{
    Task<Result<CreatePaymentResponse>> ExecuteAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default);
}
