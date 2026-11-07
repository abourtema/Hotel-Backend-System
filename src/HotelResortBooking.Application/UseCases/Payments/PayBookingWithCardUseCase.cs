using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Abstractions.Services;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.UseCases.Payments;

public sealed class PayBookingWithCardUseCase : IPayBookingWithCardUseCase
{
    private readonly IAccessContext _accessContext;
    private readonly IBookingRepository _bookingRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public PayBookingWithCardUseCase(
        IAccessContext accessContext,
        IBookingRepository bookingRepository,
        IPaymentRepository paymentRepository,
        IPaymentGateway paymentGateway,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _accessContext = accessContext;
        _bookingRepository = bookingRepository;
        _paymentRepository = paymentRepository;
        _paymentGateway = paymentGateway;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PayBookingWithCardResponse>> ExecuteAsync(
        PayBookingWithCardRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_accessContext.IsAuthenticated || _accessContext.UserId is null)
        {
            return Result.Failure<PayBookingWithCardResponse>(BusinessErrors.AuthenticationRequired());
        }

        if (string.IsNullOrWhiteSpace(request.PaymentMethodToken)
            || string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            return Result.Failure<PayBookingWithCardResponse>(BusinessErrors.InvalidPayment());
        }

        var idempotencyKey = request.IdempotencyKey.Trim();

        var existingByKey = await _paymentRepository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (existingByKey is not null)
        {
            var existingBooking = await _bookingRepository.GetByIdAsync(existingByKey.BookingId, cancellationToken);
            return Result.Success(new PayBookingWithCardResponse
            {
                PaymentId = existingByKey.Id,
                BookingId = existingByKey.BookingId,
                PaymentStatus = existingByKey.Status,
                BookingStatus = existingBooking?.Status ?? BookingStatus.Pending,
                ProviderTransactionId = existingByKey.ProviderTransactionId,
                FailureReason = existingByKey.FailureReason,
                CardBrand = existingByKey.CardBrand,
                CardLast4 = existingByKey.CardLast4
            });
        }

        var booking = await _bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);
        if (booking is null)
        {
            return Result.Failure<PayBookingWithCardResponse>(BusinessErrors.BookingNotFound(request.BookingId));
        }

        if (!request.BypassOwnershipCheck && booking.UserId != _accessContext.UserId)
        {
            return Result.Failure<PayBookingWithCardResponse>(BusinessErrors.UnauthorizedBookingAccess());
        }

        if (booking.Status is not BookingStatus.Pending || booking.TotalAmount <= 0)
        {
            return Result.Failure<PayBookingWithCardResponse>(BusinessErrors.BookingNotPayable());
        }

        var existingPayments = await _paymentRepository.GetByBookingIdAsync(booking.Id, cancellationToken);
        if (existingPayments.Any(p => p.Status == PaymentStatus.Succeeded))
        {
            return Result.Failure<PayBookingWithCardResponse>(BusinessErrors.PaymentAlreadyProcessed());
        }

        // Never store CVV — only token + optional display metadata.
        var payment = await _paymentRepository.AddAsync(new CreatePaymentModel
        {
            BookingId = booking.Id,
            UserId = booking.UserId,
            Amount = booking.TotalAmount,
            Currency = booking.Currency,
            PaymentMethod = PaymentMethodType.CreditCard,
            Provider = "stub",
            PaymentMethodToken = request.PaymentMethodToken.Trim(),
            CardBrand = request.CardBrand,
            CardLast4 = request.CardLast4,
            IdempotencyKey = idempotencyKey
        }, cancellationToken);

        var charge = await _paymentGateway.ChargeAsync(new PaymentGatewayChargeRequest
        {
            PaymentMethodToken = request.PaymentMethodToken.Trim(),
            Amount = booking.TotalAmount,
            Currency = booking.Currency,
            IdempotencyKey = idempotencyKey,
            BookingId = booking.Id
        }, cancellationToken);

        if (!charge.Succeeded)
        {
            var failed = new PaymentData
            {
                Id = payment.Id,
                BookingId = payment.BookingId,
                UserId = payment.UserId,
                Amount = payment.Amount,
                Currency = payment.Currency,
                PaymentMethod = payment.PaymentMethod,
                Status = PaymentStatus.Failed,
                Provider = charge.Provider ?? payment.Provider,
                ProviderTransactionId = charge.ProviderTransactionId,
                PaymentMethodToken = payment.PaymentMethodToken,
                CardBrand = charge.CardBrand ?? payment.CardBrand,
                CardLast4 = charge.CardLast4 ?? payment.CardLast4,
                FailureReason = charge.FailureReason ?? "Card payment failed",
                PaidAt = null,
                CreatedAt = payment.CreatedAt
            };

            await _paymentRepository.UpdateAsync(failed, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.Success(new PayBookingWithCardResponse
            {
                PaymentId = failed.Id,
                BookingId = booking.Id,
                PaymentStatus = PaymentStatus.Failed,
                BookingStatus = booking.Status,
                ProviderTransactionId = failed.ProviderTransactionId,
                FailureReason = failed.FailureReason,
                CardBrand = failed.CardBrand,
                CardLast4 = failed.CardLast4
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

        var confirmed = new BookingData
        {
            Id = booking.Id,
            BookingNumber = booking.BookingNumber,
            UserId = booking.UserId,
            PropertyId = booking.PropertyId,
            PropertyName = booking.PropertyName,
            CheckInDate = booking.CheckInDate,
            CheckOutDate = booking.CheckOutDate,
            Adults = booking.Adults,
            Children = booking.Children,
            RoomsCount = booking.RoomsCount,
            Status = BookingStatus.Confirmed,
            Currency = booking.Currency,
            SubTotal = booking.SubTotal,
            UpgradeTotal = booking.UpgradeTotal,
            DiscountAmount = booking.DiscountAmount,
            PointsRedeemed = booking.PointsRedeemed,
            TaxAmount = booking.TaxAmount,
            TotalAmount = booking.TotalAmount,
            CancellationDeadline = booking.CancellationDeadline,
            ConfirmedAt = _dateTimeProvider.UtcNow,
            CancelledAt = booking.CancelledAt,
            CreatedAt = booking.CreatedAt,
            SpecialRequests = booking.SpecialRequests,
            Guests = booking.Guests,
            Rooms = booking.Rooms,
            Upgrades = booking.Upgrades
        };

        await _bookingRepository.UpdateAsync(confirmed, cancellationToken);
        await _bookingRepository.AddStatusHistoryAsync(
            booking.Id,
            BookingStatus.Pending,
            BookingStatus.Confirmed,
            "Card payment succeeded",
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new PayBookingWithCardResponse
        {
            PaymentId = succeeded.Id,
            BookingId = booking.Id,
            PaymentStatus = PaymentStatus.Succeeded,
            BookingStatus = BookingStatus.Confirmed,
            ProviderTransactionId = succeeded.ProviderTransactionId,
            CardBrand = succeeded.CardBrand,
            CardLast4 = succeeded.CardLast4
        });
    }
}
