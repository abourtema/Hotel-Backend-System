using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.UseCases.Payments;

public sealed class ConfirmPayAtHotelPaymentUseCase : IConfirmPayAtHotelPaymentUseCase
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmPayAtHotelPaymentUseCase(
        IPaymentRepository paymentRepository,
        IBookingRepository bookingRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _paymentRepository = paymentRepository;
        _bookingRepository = bookingRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ConfirmPayAtHotelPaymentResponse>> ExecuteAsync(
        ConfirmPayAtHotelPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(request.PaymentId, cancellationToken);
        if (payment is null)
        {
            return Result.Failure<ConfirmPayAtHotelPaymentResponse>(
                BusinessErrors.PaymentNotFound(request.PaymentId));
        }

        if (payment.PaymentMethod != PaymentMethodType.PayAtHotel)
        {
            return Result.Failure<ConfirmPayAtHotelPaymentResponse>(BusinessErrors.PaymentNotConfirmable());
        }

        var booking = await _bookingRepository.GetByIdAsync(payment.BookingId, cancellationToken);

        if (payment.Status == PaymentStatus.Succeeded)
        {
            return Result.Success(Map(payment, booking?.Status));
        }

        if (payment.Status is not (PaymentStatus.Pending or PaymentStatus.Processing))
        {
            return Result.Failure<ConfirmPayAtHotelPaymentResponse>(BusinessErrors.PaymentNotConfirmable());
        }

        var paidAt = _dateTimeProvider.UtcNow;
        var succeeded = new PaymentData
        {
            Id = payment.Id,
            BookingId = payment.BookingId,
            UserId = payment.UserId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            PaymentMethod = payment.PaymentMethod,
            Status = PaymentStatus.Succeeded,
            Provider = payment.Provider ?? "hotel",
            ProviderTransactionId = payment.ProviderTransactionId,
            PaymentMethodToken = payment.PaymentMethodToken,
            CardBrand = payment.CardBrand,
            CardLast4 = payment.CardLast4,
            FailureReason = null,
            PaidAt = paidAt,
            CreatedAt = payment.CreatedAt
        };

        await _paymentRepository.UpdateAsync(succeeded, cancellationToken);

        BookingStatus? bookingStatus = booking?.Status;
        if (booking is not null && booking.Status == BookingStatus.Pending)
        {
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
                ConfirmedAt = paidAt,
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
                "Pay at hotel collected",
                cancellationToken);
            bookingStatus = BookingStatus.Confirmed;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(Map(succeeded, bookingStatus));
    }

    private static ConfirmPayAtHotelPaymentResponse Map(PaymentData payment, BookingStatus? bookingStatus) => new()
    {
        PaymentId = payment.Id,
        BookingId = payment.BookingId,
        Status = payment.Status,
        BookingStatus = bookingStatus,
        PaidAt = payment.PaidAt
    };
}
