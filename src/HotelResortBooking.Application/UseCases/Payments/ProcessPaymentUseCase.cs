using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Abstractions.Services;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.UseCases.Payments;

public sealed class ProcessPaymentUseCase : IProcessPaymentUseCase
{
    private readonly IAccessContext _accessContext;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ProcessPaymentUseCase(
        IAccessContext accessContext,
        IPaymentRepository paymentRepository,
        IBookingRepository bookingRepository,
        IPaymentGateway paymentGateway,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _accessContext = accessContext;
        _paymentRepository = paymentRepository;
        _bookingRepository = bookingRepository;
        _paymentGateway = paymentGateway;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ProcessPaymentResponse>> ExecuteAsync(
        ProcessPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_accessContext.IsAuthenticated || _accessContext.UserId is null)
        {
            return Result.Failure<ProcessPaymentResponse>(BusinessErrors.AuthenticationRequired());
        }

        var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);
        if (payment is null)
        {
            return Result.Failure<ProcessPaymentResponse>(
                BusinessErrors.Validation("PaymentNotFound", $"Payment '{request.PaymentId}' was not found."));
        }

        if (!request.BypassOwnershipCheck && payment.UserId != _accessContext.UserId)
        {
            return Result.Failure<ProcessPaymentResponse>(BusinessErrors.UnauthorizedPaymentAccess());
        }

        if (payment.Status == PaymentStatus.Succeeded)
        {
            var booking = await _bookingRepository.GetByIdAsync(payment.BookingId, cancellationToken);
            return Result.Success(new ProcessPaymentResponse
            {
                PaymentId = payment.Id,
                BookingId = payment.BookingId,
                Status = payment.Status,
                ProviderTransactionId = payment.ProviderTransactionId,
                BookingStatus = booking?.Status
            });
        }

        if (string.IsNullOrWhiteSpace(request.IdempotencyKey)
            && string.IsNullOrWhiteSpace(request.PaymentMethodToken)
            && string.IsNullOrWhiteSpace(request.SavedProviderPaymentMethodId)
            && string.IsNullOrWhiteSpace(payment.PaymentMethodToken))
        {
            return Result.Failure<ProcessPaymentResponse>(BusinessErrors.InvalidPayment());
        }

        var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? payment.Id.ToString("N")
            : request.IdempotencyKey.Trim();

        var processing = ClonePayment(payment, PaymentStatus.Processing, null, null, null, null, null);
        await _paymentRepository.UpdateAsync(processing, cancellationToken);

        var charge = await _paymentGateway.ChargeAsync(new PaymentGatewayChargeRequest
        {
            PaymentMethodToken = request.PaymentMethodToken ?? payment.PaymentMethodToken,
            SavedProviderPaymentMethodId = request.SavedProviderPaymentMethodId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            IdempotencyKey = idempotencyKey,
            BookingId = payment.BookingId
        }, cancellationToken);

        if (!charge.Succeeded)
        {
            var failed = ClonePayment(
                payment,
                PaymentStatus.Failed,
                charge.Provider,
                charge.ProviderTransactionId,
                charge.CardBrand,
                charge.CardLast4,
                charge.FailureReason);

            await _paymentRepository.UpdateAsync(failed, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(new ProcessPaymentResponse
            {
                PaymentId = payment.Id,
                BookingId = payment.BookingId,
                Status = PaymentStatus.Failed,
                ProviderTransactionId = charge.ProviderTransactionId,
                FailureReason = charge.FailureReason
            });
        }

        var succeeded = new PaymentData
        {
            Id = payment.Id,
            BookingId = payment.BookingId,
            UserId = payment.UserId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            PaymentMethod = payment.PaymentMethod,
            Status = PaymentStatus.Succeeded,
            Provider = charge.Provider ?? payment.Provider,
            ProviderTransactionId = charge.ProviderTransactionId,
            PaymentMethodToken = payment.PaymentMethodToken,
            CardBrand = charge.CardBrand ?? payment.CardBrand,
            CardLast4 = charge.CardLast4 ?? payment.CardLast4,
            FailureReason = null,
            PaidAt = _dateTimeProvider.UtcNow,
            CreatedAt = payment.CreatedAt
        };

        await _paymentRepository.UpdateAsync(succeeded, cancellationToken);

        var bookingData = await _bookingRepository.GetByIdAsync(payment.BookingId, cancellationToken);
        BookingStatus? bookingStatus = bookingData?.Status;
        if (bookingData is not null && bookingData.Status == BookingStatus.Pending)
        {
            var confirmed = new BookingData
            {
                Id = bookingData.Id,
                BookingNumber = bookingData.BookingNumber,
                UserId = bookingData.UserId,
                PropertyId = bookingData.PropertyId,
                PropertyName = bookingData.PropertyName,
                CheckInDate = bookingData.CheckInDate,
                CheckOutDate = bookingData.CheckOutDate,
                Adults = bookingData.Adults,
                Children = bookingData.Children,
                RoomsCount = bookingData.RoomsCount,
                Status = BookingStatus.Confirmed,
                Currency = bookingData.Currency,
                SubTotal = bookingData.SubTotal,
                UpgradeTotal = bookingData.UpgradeTotal,
                DiscountAmount = bookingData.DiscountAmount,
                PointsRedeemed = bookingData.PointsRedeemed,
                TaxAmount = bookingData.TaxAmount,
                TotalAmount = bookingData.TotalAmount,
                CancellationDeadline = bookingData.CancellationDeadline,
                ConfirmedAt = _dateTimeProvider.UtcNow,
                CancelledAt = bookingData.CancelledAt,
                CreatedAt = bookingData.CreatedAt,
                SpecialRequests = bookingData.SpecialRequests,
                Guests = bookingData.Guests,
                Rooms = bookingData.Rooms,
                Upgrades = bookingData.Upgrades
            };

            await _bookingRepository.UpdateAsync(confirmed, cancellationToken);
            await _bookingRepository.AddStatusHistoryAsync(
                bookingData.Id,
                BookingStatus.Pending,
                BookingStatus.Confirmed,
                "Payment succeeded",
                cancellationToken);
            bookingStatus = BookingStatus.Confirmed;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new ProcessPaymentResponse
        {
            PaymentId = payment.Id,
            BookingId = payment.BookingId,
            Status = PaymentStatus.Succeeded,
            ProviderTransactionId = charge.ProviderTransactionId,
            BookingStatus = bookingStatus
        });
    }

    private static PaymentData ClonePayment(
        PaymentData payment,
        PaymentStatus status,
        string? provider,
        string? providerTransactionId,
        string? cardBrand,
        string? cardLast4,
        string? failureReason) => new()
    {
        Id = payment.Id,
        BookingId = payment.BookingId,
        UserId = payment.UserId,
        Amount = payment.Amount,
        Currency = payment.Currency,
        PaymentMethod = payment.PaymentMethod,
        Status = status,
        Provider = provider ?? payment.Provider,
        ProviderTransactionId = providerTransactionId ?? payment.ProviderTransactionId,
        PaymentMethodToken = payment.PaymentMethodToken,
        CardBrand = cardBrand ?? payment.CardBrand,
        CardLast4 = cardLast4 ?? payment.CardLast4,
        FailureReason = failureReason,
        PaidAt = payment.PaidAt,
        CreatedAt = payment.CreatedAt
    };
}
