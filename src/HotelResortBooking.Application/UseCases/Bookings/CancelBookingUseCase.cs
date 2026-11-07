using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Abstractions.Services;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.UseCases.Bookings;

public sealed class CancelBookingUseCase : ICancelBookingUseCase
{
    private readonly IAccessContext _accessContext;
    private readonly IBookingRepository _bookingRepository;
    private readonly IRoomAvailabilityService _availabilityService;
    private readonly ILoyaltyRepository _loyaltyRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CancelBookingUseCase(
        IAccessContext accessContext,
        IBookingRepository bookingRepository,
        IRoomAvailabilityService availabilityService,
        ILoyaltyRepository loyaltyRepository,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _accessContext = accessContext;
        _bookingRepository = bookingRepository;
        _availabilityService = availabilityService;
        _loyaltyRepository = loyaltyRepository;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CancelBookingResponse>> ExecuteAsync(
        CancelBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_accessContext.IsAuthenticated || _accessContext.UserId is null)
        {
            return Result.Failure<CancelBookingResponse>(BusinessErrors.AuthenticationRequired());
        }

        var booking = await _bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);
        if (booking is null)
        {
            return Result.Failure<CancelBookingResponse>(BusinessErrors.BookingNotFound(request.BookingId));
        }

        if (!request.BypassOwnershipCheck && booking.UserId != _accessContext.UserId)
        {
            return Result.Failure<CancelBookingResponse>(BusinessErrors.UnauthorizedBookingAccess());
        }

        if (booking.Status is not (BookingStatus.Pending or BookingStatus.Confirmed or BookingStatus.Completed))
        {
            return Result.Failure<CancelBookingResponse>(BusinessErrors.BookingNotCancellable());
        }

        if (booking.CancellationDeadline is not null
            && _dateTimeProvider.UtcNow > booking.CancellationDeadline.Value)
        {
            return Result.Failure<CancelBookingResponse>(BusinessErrors.BookingNotCancellable());
        }

        var oldStatus = booking.Status;
        var cancelledAt = _dateTimeProvider.UtcNow;

        var updated = new BookingData
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
            Status = BookingStatus.Cancelled,
            Currency = booking.Currency,
            SubTotal = booking.SubTotal,
            UpgradeTotal = booking.UpgradeTotal,
            DiscountAmount = booking.DiscountAmount,
            PointsRedeemed = booking.PointsRedeemed,
            TaxAmount = booking.TaxAmount,
            TotalAmount = booking.TotalAmount,
            CancellationDeadline = booking.CancellationDeadline,
            ConfirmedAt = booking.ConfirmedAt,
            CancelledAt = cancelledAt,
            CreatedAt = booking.CreatedAt,
            SpecialRequests = booking.SpecialRequests,
            Guests = booking.Guests,
            Rooms = booking.Rooms,
            Upgrades = booking.Upgrades
        };

        await _bookingRepository.UpdateAsync(updated, cancellationToken);
        await _bookingRepository.AddStatusHistoryAsync(
            booking.Id,
            oldStatus,
            BookingStatus.Cancelled,
            request.Reason ?? "Cancelled by user",
            cancellationToken);

        foreach (var room in booking.Rooms)
        {
            await _availabilityService.ReleaseAsync(new AvailabilityCheckRequest
            {
                RoomTypeId = room.RoomTypeId,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                RoomsRequested = room.Quantity
            }, cancellationToken);
        }

        if (booking.PointsRedeemed > 0 && booking.UserId is not null)
        {
            await _loyaltyRepository.RestoreAsync(
                booking.UserId.Value,
                booking.Id,
                booking.PointsRedeemed,
                "Points restored after cancellation",
                cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CancelBookingResponse
        {
            Booking = BookingDto.From(updated)
        });
    }
}
