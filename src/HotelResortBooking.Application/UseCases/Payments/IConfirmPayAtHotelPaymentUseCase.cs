using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.UseCases.Payments;

public sealed class ConfirmPayAtHotelPaymentRequest
{
    public Guid PaymentId { get; init; }
}

public sealed class ConfirmPayAtHotelPaymentResponse
{
    public Guid PaymentId { get; init; }

    public Guid BookingId { get; init; }

    public PaymentStatus Status { get; init; }

    public BookingStatus? BookingStatus { get; init; }

    public DateTimeOffset? PaidAt { get; init; }
}

public interface IConfirmPayAtHotelPaymentUseCase
{
    Task<Result<ConfirmPayAtHotelPaymentResponse>> ExecuteAsync(
        ConfirmPayAtHotelPaymentRequest request,
        CancellationToken cancellationToken = default);
}
