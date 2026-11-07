using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.UseCases.Bookings;

public sealed class CompleteBookingRequest
{
    public Guid BookingId { get; init; }

    public string? Reason { get; init; }

    public bool BypassOwnershipCheck { get; init; }
}

public sealed class CompleteBookingResponse
{
    public BookingDto Booking { get; init; } = null!;
}

public interface ICompleteBookingUseCase
{
    Task<Result<CompleteBookingResponse>> ExecuteAsync(
        CompleteBookingRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class CompleteBookingUseCase : ICompleteBookingUseCase
{
    private readonly IAccessContext _accessContext;
    private readonly IBookingRepository _bookingRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteBookingUseCase(
        IAccessContext accessContext,
        IBookingRepository bookingRepository,
        IUnitOfWork unitOfWork)
    {
        _accessContext = accessContext;
        _bookingRepository = bookingRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CompleteBookingResponse>> ExecuteAsync(
        CompleteBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_accessContext.IsAuthenticated || _accessContext.UserId is null)
        {
            return Result.Failure<CompleteBookingResponse>(BusinessErrors.AuthenticationRequired());
        }

        var booking = await _bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);
        if (booking is null)
        {
            return Result.Failure<CompleteBookingResponse>(BusinessErrors.BookingNotFound(request.BookingId));
        }

        if (!request.BypassOwnershipCheck)
        {
            return Result.Failure<CompleteBookingResponse>(BusinessErrors.UnauthorizedBookingAccess());
        }

        if (booking.Status != BookingStatus.Confirmed)
        {
            return Result.Failure<CompleteBookingResponse>(BusinessErrors.BookingNotCompletable());
        }

        var updated = WithStatus(booking, BookingStatus.Completed);

        await _bookingRepository.UpdateAsync(updated, cancellationToken);
        await _bookingRepository.AddStatusHistoryAsync(
            booking.Id,
            BookingStatus.Confirmed,
            BookingStatus.Completed,
            string.IsNullOrWhiteSpace(request.Reason) ? "Completed by management" : request.Reason.Trim(),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CompleteBookingResponse
        {
            Booking = BookingDto.From(updated)
        });
    }

    private static BookingData WithStatus(BookingData booking, BookingStatus status)
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
            ConfirmedAt = booking.ConfirmedAt,
            CancelledAt = booking.CancelledAt,
            CreatedAt = booking.CreatedAt,
            SpecialRequests = booking.SpecialRequests,
            Guests = booking.Guests,
            Rooms = booking.Rooms,
            Upgrades = booking.Upgrades
        };
}
