using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Abstractions.Services;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.UseCases.Payments;

public sealed class CreatePaymentUseCase : ICreatePaymentUseCase
{
    private readonly IAccessContext _accessContext;
    private readonly IBookingRepository _bookingRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CreatePaymentUseCase(
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

    public async Task<Result<CreatePaymentResponse>> ExecuteAsync(
        CreatePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_accessContext.IsAuthenticated || _accessContext.UserId is null)
        {
            return Result.Failure<CreatePaymentResponse>(BusinessErrors.AuthenticationRequired());
        }

        var booking = await _bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);
        if (booking is null)
        {
            return Result.Failure<CreatePaymentResponse>(BusinessErrors.BookingNotFound(request.BookingId));
        }

        if (!request.BypassOwnershipCheck && booking.UserId != _accessContext.UserId)
        {
            return Result.Failure<CreatePaymentResponse>(BusinessErrors.UnauthorizedBookingAccess());
        }

        if (booking.Status is not (BookingStatus.Pending or BookingStatus.Confirmed)
            || booking.TotalAmount <= 0)
        {
            return Result.Failure<CreatePaymentResponse>(BusinessErrors.BookingNotPayable());
        }

        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existing = await _paymentRepository.GetByIdempotencyKeyAsync(
                request.IdempotencyKey.Trim(),
                cancellationToken);
            if (existing is not null)
            {
                return Result.Success(Map(existing));
            }
        }

        var successful = await _paymentRepository.GetByBookingIdAsync(request.BookingId, cancellationToken);
        if (successful.Any(p => p.Status == PaymentStatus.Succeeded))
        {
            return Result.Failure<CreatePaymentResponse>(BusinessErrors.PaymentAlreadyProcessed());
        }

        var isPayAtHotel = request.PaymentMethod == PaymentMethodType.PayAtHotel;
        var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? null
            : request.IdempotencyKey.Trim();

        var payment = await _paymentRepository.AddAsync(new CreatePaymentModel
        {
            BookingId = booking.Id,
            UserId = booking.UserId,
            Amount = booking.TotalAmount,
            Currency = booking.Currency,
            PaymentMethod = request.PaymentMethod,
            Provider = isPayAtHotel ? "hotel" : "stub",
            PaymentMethodToken = request.PaymentMethodToken,
            CardBrand = request.CardBrand,
            CardLast4 = request.CardLast4,
            IdempotencyKey = idempotencyKey
        }, cancellationToken);

        if (isPayAtHotel)
        {
            await ConfirmPendingBookingAsync(booking, "Pay at hotel selected", cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(Map(payment));
        }

        var charge = await _paymentGateway.ChargeAsync(new PaymentGatewayChargeRequest
        {
            PaymentMethodToken = request.PaymentMethodToken,
            Amount = booking.TotalAmount,
            Currency = booking.Currency,
            IdempotencyKey = idempotencyKey ?? payment.Id.ToString("N"),
            BookingId = booking.Id
        }, cancellationToken);

        if (!charge.Succeeded)
        {
            var failed = WithPaymentStatus(
                payment,
                PaymentStatus.Failed,
                charge.Provider,
                charge.ProviderTransactionId,
                charge.CardBrand,
                charge.CardLast4,
                charge.FailureReason ?? "Payment failed",
                paidAt: null);

            await _paymentRepository.UpdateAsync(failed, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success(Map(failed));
        }

        var succeeded = WithPaymentStatus(
            payment,
            PaymentStatus.Succeeded,
            charge.Provider,
            charge.ProviderTransactionId,
            charge.CardBrand,
            charge.CardLast4,
            failureReason: null,
            paidAt: _dateTimeProvider.UtcNow);

        await _paymentRepository.UpdateAsync(succeeded, cancellationToken);
        await ConfirmPendingBookingAsync(booking, "Payment succeeded", cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(Map(succeeded));
    }

    private async Task ConfirmPendingBookingAsync(
        BookingData booking,
        string reason,
        CancellationToken cancellationToken)
    {
        if (booking.Status != BookingStatus.Pending)
        {
            return;
        }

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
            reason,
            cancellationToken);
    }

    private static PaymentData WithPaymentStatus(
        PaymentData payment,
        PaymentStatus status,
        string? provider,
        string? providerTransactionId,
        string? cardBrand,
        string? cardLast4,
        string? failureReason,
        DateTimeOffset? paidAt) => new()
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
        PaidAt = paidAt,
        CreatedAt = payment.CreatedAt
    };

    private static CreatePaymentResponse Map(PaymentData payment) => new()
    {
        PaymentId = payment.Id,
        BookingId = payment.BookingId,
        Amount = payment.Amount,
        Currency = payment.Currency,
        Status = payment.Status,
        PaymentMethod = payment.PaymentMethod
    };
}
