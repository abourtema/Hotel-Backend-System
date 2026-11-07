using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.UseCases.Bookings;

public sealed class ConfirmBookingRequest
{
    public Guid BookingId { get; init; }

    public string? Reason { get; init; }

    public bool BypassOwnershipCheck { get; init; }
}

public sealed class ConfirmBookingResponse
{
    public BookingDto Booking { get; init; } = null!;
}

public interface IConfirmBookingUseCase
{
    Task<Result<ConfirmBookingResponse>> ExecuteAsync(
        ConfirmBookingRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class ConfirmBookingUseCase : IConfirmBookingUseCase
{
    private readonly IAccessContext _accessContext;
    private readonly IBookingRepository _bookingRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public ConfirmBookingUseCase(
        IAccessContext accessContext,
        IBookingRepository bookingRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _accessContext = accessContext;
        _bookingRepository = bookingRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ConfirmBookingResponse>> ExecuteAsync(
        ConfirmBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_accessContext.IsAuthenticated || _accessContext.UserId is null)
        {
            return Result.Failure<ConfirmBookingResponse>(BusinessErrors.AuthenticationRequired());
        }

        var booking = await _bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);
        if (booking is null)
        {
            return Result.Failure<ConfirmBookingResponse>(BusinessErrors.BookingNotFound(request.BookingId));
        }

        if (!request.BypassOwnershipCheck)
        {
            return Result.Failure<ConfirmBookingResponse>(BusinessErrors.UnauthorizedBookingAccess());
        }

        if (booking.Status != BookingStatus.Pending)
        {
            return Result.Failure<ConfirmBookingResponse>(BusinessErrors.BookingNotConfirmable());
        }

        var confirmedAt = _dateTimeProvider.UtcNow;
        var updated = WithStatus(booking, BookingStatus.Confirmed, confirmedAt);

        await _bookingRepository.UpdateAsync(updated, cancellationToken);
        await _bookingRepository.AddStatusHistoryAsync(
            booking.Id,
            BookingStatus.Pending,
            BookingStatus.Confirmed,
            string.IsNullOrWhiteSpace(request.Reason) ? "Confirmed by management" : request.Reason.Trim(),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new ConfirmBookingResponse
        {
            Booking = BookingDto.From(updated)
        });
    }

    private static BookingData WithStatus(BookingData booking, BookingStatus status, DateTimeOffset confirmedAt)
        => new()
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
            Status = status,
            Currency = booking.Currency,
            SubTotal = booking.SubTotal,
            UpgradeTotal = booking.UpgradeTotal,
            DiscountAmount = booking.DiscountAmount,
            PointsRedeemed = booking.PointsRedeemed,
            TaxAmount = booking.TaxAmount,
            TotalAmount = booking.TotalAmount,
            CancellationDeadline = booking.CancellationDeadline,
            ConfirmedAt = confirmedAt,
            CancelledAt = booking.CancelledAt,
            CreatedAt = booking.CreatedAt,
            SpecialRequests = booking.SpecialRequests,
            Guests = booking.Guests,
            Rooms = booking.Rooms,
            Upgrades = booking.Upgrades
        };
}
