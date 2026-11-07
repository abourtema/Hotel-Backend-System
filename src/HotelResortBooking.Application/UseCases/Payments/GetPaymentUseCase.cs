using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Payments;

public sealed class GetPaymentUseCase : IGetPaymentUseCase
{
    private readonly IAccessContext _accessContext;
    private readonly IPaymentRepository _paymentRepository;

    public GetPaymentUseCase(IAccessContext accessContext, IPaymentRepository paymentRepository)
    {
        _accessContext = accessContext;
        _paymentRepository = paymentRepository;
    }

    public async Task<Result<GetPaymentResponse>> ExecuteAsync(
        GetPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_accessContext.IsAuthenticated || _accessContext.UserId is null)
        {
            return Result.Failure<GetPaymentResponse>(BusinessErrors.AuthenticationRequired());
        }

        var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);
        if (payment is null)
        {
            return Result.Failure<GetPaymentResponse>(
                BusinessErrors.Validation("PaymentNotFound", $"Payment '{request.PaymentId}' was not found."));
        }

        if (!request.BypassOwnershipCheck && payment.UserId != _accessContext.UserId)
        {
            return Result.Failure<GetPaymentResponse>(BusinessErrors.UnauthorizedPaymentAccess());
        }

        return Result.Success(new GetPaymentResponse
        {
            PaymentId = payment.Id,
            BookingId = payment.BookingId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            PaymentMethod = payment.PaymentMethod,
            Status = payment.Status,
            Provider = payment.Provider,
            ProviderTransactionId = payment.ProviderTransactionId,
            CardBrand = payment.CardBrand,
            CardLast4 = payment.CardLast4,
            FailureReason = payment.FailureReason,
            PaidAt = payment.PaidAt,
            CreatedAt = payment.CreatedAt
        });
    }
}
