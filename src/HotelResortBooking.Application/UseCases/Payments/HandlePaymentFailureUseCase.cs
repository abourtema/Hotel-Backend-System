using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.UseCases.Payments;

public sealed class HandlePaymentFailureUseCase : IHandlePaymentFailureUseCase
{
    private readonly IAccessContext _accessContext;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public HandlePaymentFailureUseCase(
        IAccessContext accessContext,
        IPaymentRepository paymentRepository,
        IBookingRepository bookingRepository,
        IUnitOfWork unitOfWork)
    {
        _accessContext = accessContext;
        _paymentRepository = paymentRepository;
        _bookingRepository = bookingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<HandlePaymentFailureResponse>> ExecuteAsync(
        HandlePaymentFailureRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_accessContext.IsAuthenticated || _accessContext.UserId is null)
        {
            return Result.Failure<HandlePaymentFailureResponse>(BusinessErrors.AuthenticationRequired());
        }

        var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);
        if (payment is null)
        {
            return Result.Failure<HandlePaymentFailureResponse>(
                BusinessErrors.Validation("PaymentNotFound", $"Payment '{request.PaymentId}' was not found."));
        }

        if (!request.BypassOwnershipCheck && payment.UserId != _accessContext.UserId)
        {
            return Result.Failure<HandlePaymentFailureResponse>(BusinessErrors.UnauthorizedPaymentAccess());
        }

        if (payment.Status == PaymentStatus.Succeeded)
        {
            return Result.Failure<HandlePaymentFailureResponse>(BusinessErrors.PaymentAlreadyProcessed());
        }

        var failed = new PaymentData
        {
            Id = payment.Id,
            BookingId = payment.BookingId,
            UserId = payment.UserId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            PaymentMethod = payment.PaymentMethod,
            Status = PaymentStatus.Failed,
            Provider = payment.Provider,
            ProviderTransactionId = payment.ProviderTransactionId,
            PaymentMethodToken = payment.PaymentMethodToken,
            CardBrand = payment.CardBrand,
            CardLast4 = payment.CardLast4,
            FailureReason = string.IsNullOrWhiteSpace(request.FailureReason)
                ? "Payment failed"
                : request.FailureReason.Trim(),
            PaidAt = payment.PaidAt,
            CreatedAt = payment.CreatedAt
        };

        await _paymentRepository.UpdateAsync(failed, cancellationToken);

        var booking = await _bookingRepository.GetByIdAsync(payment.BookingId, cancellationToken);
        if (booking is not null && booking.Status == BookingStatus.Pending)
        {
            await _bookingRepository.AddStatusHistoryAsync(
                booking.Id,
                booking.Status,
                booking.Status,
                failed.FailureReason,
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new HandlePaymentFailureResponse
        {
            PaymentId = failed.Id,
            BookingId = failed.BookingId,
            Status = failed.Status,
            FailureReason = failed.FailureReason
        });
    }
}
