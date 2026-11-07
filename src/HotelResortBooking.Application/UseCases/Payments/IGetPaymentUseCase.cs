using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Payments;

public interface IGetPaymentUseCase
{
    Task<Result<GetPaymentResponse>> ExecuteAsync(
        GetPaymentRequest request,
        CancellationToken cancellationToken = default);
}
