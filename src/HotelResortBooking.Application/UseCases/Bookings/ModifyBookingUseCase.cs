using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Abstractions.Services;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.Domain.Rules;

namespace HotelResortBooking.Application.UseCases.Bookings;

public sealed class ModifyBookingUseCase : IModifyBookingUseCase
{
    private readonly IAccessContext _accessContext;
    private readonly IBookingRepository _bookingRepository;
    private readonly IRoomTypeRepository _roomTypeRepository;
    private readonly IUpgradeRepository _upgradeRepository;
    private readonly IRoomAvailabilityService _availabilityService;
    private readonly IPricingService _pricingService;
    private readonly IUnitOfWork _unitOfWork;

    public ModifyBookingUseCase(
        IAccessContext accessContext,
        IBookingRepository bookingRepository,
        IRoomTypeRepository roomTypeRepository,
        IUpgradeRepository upgradeRepository,
        IRoomAvailabilityService availabilityService,
        IPricingService pricingService,
        IUnitOfWork unitOfWork)
    {
        _accessContext = accessContext;
        _bookingRepository = bookingRepository;
        _roomTypeRepository = roomTypeRepository;
        _upgradeRepository = upgradeRepository;
        _availabilityService = availabilityService;
        _pricingService = pricingService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ModifyBookingResponse>> ExecuteAsync(
        ModifyBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_accessContext.IsAuthenticated || _accessContext.UserId is null)
        {
            return Result.Failure<ModifyBookingResponse>(BusinessErrors.AuthenticationRequired());
        }

        if (new DateRangeMustBeValidRule(request.CheckInDate, request.CheckOutDate).IsBroken())
        {
            return Result.Failure<ModifyBookingResponse>(BusinessErrors.InvalidDateRange());
        }

        if (new GuestCountMustBeValidRule(request.Adults, request.Children).IsBroken())
        {
            return Result.Failure<ModifyBookingResponse>(BusinessErrors.InvalidGuestCount());
        }

        if (new RoomCountMustBeValidRule(request.Rooms).IsBroken())
        {
            return Result.Failure<ModifyBookingResponse>(BusinessErrors.InvalidRoomCount());
        }

        var booking = await _bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);
        if (booking is null)
        {
            return Result.Failure<ModifyBookingResponse>(BusinessErrors.BookingNotFound(request.BookingId));
        }

        if (!request.BypassOwnershipCheck && booking.UserId != _accessContext.UserId)
        {
            return Result.Failure<ModifyBookingResponse>(BusinessErrors.UnauthorizedBookingAccess());
        }

        if (booking.Status is not (BookingStatus.Pending or BookingStatus.Confirmed or BookingStatus.Completed))
        {
            return Result.Failure<ModifyBookingResponse>(BusinessErrors.BookingNotModifiable());
        }

        var existingRoom = booking.Rooms.FirstOrDefault();
        if (existingRoom is null)
        {
            return Result.Failure<ModifyBookingResponse>(BusinessErrors.BookingNotModifiable());
        }

        var roomTypeId = request.RoomTypeId ?? existingRoom.RoomTypeId;
        var room = await _roomTypeRepository.GetByIdAsync(roomTypeId, cancellationToken);
        if (room is null || room.PropertyId != booking.PropertyId)
        {
            return Result.Failure<ModifyBookingResponse>(BusinessErrors.RoomNotFound(roomTypeId));
        }

        if (new OccupancyMustFitRoomRule(
                request.Adults,
                request.Children,
                request.Rooms,
                room.MaxAdults,
                room.MaxChildren,
                room.MaxGuests).IsBroken())
        {
            return Result.Failure<ModifyBookingResponse>(BusinessErrors.InvalidOccupancy());
        }

        var stayUnchanged = roomTypeId == existingRoom.RoomTypeId
            && request.CheckInDate == booking.CheckInDate
            && request.CheckOutDate == booking.CheckOutDate
            && request.Rooms == existingRoom.Quantity;

        if (!stayUnchanged)
        {
            // Release previous inventory, then reserve new selection.
            await _availabilityService.ReleaseAsync(new AvailabilityCheckRequest
            {
                RoomTypeId = existingRoom.RoomTypeId,
                CheckInDate = booking.CheckInDate,
                CheckOutDate = booking.CheckOutDate,
                RoomsRequested = existingRoom.Quantity
            }, cancellationToken);

            var newAvailability = new AvailabilityCheckRequest
            {
                RoomTypeId = roomTypeId,
                CheckInDate = request.CheckInDate,
                CheckOutDate = request.CheckOutDate,
                RoomsRequested = request.Rooms
            };

            var check = await _availabilityService.CheckAsync(newAvailability, cancellationToken);
            if (!check.IsAvailable)
            {
                // Best-effort restore of previous reservation.
                await _availabilityService.ReserveAsync(new AvailabilityCheckRequest
                {
                    RoomTypeId = existingRoom.RoomTypeId,
                    CheckInDate = booking.CheckInDate,
                    CheckOutDate = booking.CheckOutDate,
                    RoomsRequested = existingRoom.Quantity
                }, cancellationToken);

                return Result.Failure<ModifyBookingResponse>(BusinessErrors.RoomUnavailable());
            }

            var reserved = await _availabilityService.ReserveAsync(newAvailability, cancellationToken);
            if (!reserved)
            {
                await _availabilityService.ReserveAsync(new AvailabilityCheckRequest
                {
                    RoomTypeId = existingRoom.RoomTypeId,
                    CheckInDate = booking.CheckInDate,
                    CheckOutDate = booking.CheckOutDate,
                    RoomsRequested = existingRoom.Quantity
                }, cancellationToken);

                return Result.Failure<ModifyBookingResponse>(BusinessErrors.RoomUnavailable());
            }
        }

        var upgradeSelections = request.Upgrades
            ?? booking.Upgrades.Select(u => new BookingUpgradeSelection
            {
                UpgradeOptionId = u.UpgradeOptionId,
                Quantity = u.Quantity
            }).ToList();

        var upgradeModels = new List<CreateBookingUpgradeModel>();
        var upgradeLines = new List<UpgradePriceLine>();
        var upgradeData = new List<BookingUpgradeData>();

        foreach (var selection in upgradeSelections)
        {
            if (selection.Quantity <= 0)
            {
                continue;
            }

            var upgrade = await _upgradeRepository.GetByIdAsync(selection.UpgradeOptionId, cancellationToken);
            if (upgrade is null)
            {
                return Result.Failure<ModifyBookingResponse>(BusinessErrors.UpgradeNotFound(selection.UpgradeOptionId));
            }

            upgradeLines.Add(new UpgradePriceLine
            {
                UpgradeOptionId = upgrade.Id,
                UnitPrice = upgrade.Price,
                Quantity = selection.Quantity
            });

            upgradeModels.Add(new CreateBookingUpgradeModel
            {
                UpgradeOptionId = upgrade.Id,
                Quantity = selection.Quantity,
                UnitPrice = upgrade.Price,
                TotalPrice = upgrade.Price * selection.Quantity
            });

            upgradeData.Add(new BookingUpgradeData
            {
                Id = Guid.Empty,
                UpgradeOptionId = upgrade.Id,
                Name = upgrade.Name,
                Quantity = selection.Quantity,
                UnitPrice = upgrade.Price,
                TotalPrice = upgrade.Price * selection.Quantity,
                IsMandatory = upgrade.IsMandatory
            });
        }

        var nights = _pricingService.CalculateNights(request.CheckInDate, request.CheckOutDate);
        var pricing = _pricingService.Calculate(new PricingRequest
        {
            RoomPricePerNight = room.PricePerNight,
            Nights = nights,
            RoomQuantity = request.Rooms,
            Upgrades = upgradeLines,
            PointsToRedeem = booking.PointsRedeemed,
            Currency = room.Currency
        });

        var roomData = new BookingRoomData
        {
            Id = existingRoom.Id,
            RoomTypeId = room.RoomTypeId,
            RoomTypeName = room.Name,
            Quantity = request.Rooms,
            PricePerNight = room.PricePerNight,
            Nights = nights,
            SubTotal = pricing.SubTotal
        };

        var updated = new BookingData
        {
            Id = booking.Id,
            BookingNumber = booking.BookingNumber,
            UserId = booking.UserId,
            PropertyId = booking.PropertyId,
            PropertyName = booking.PropertyName,
            CheckInDate = request.CheckInDate,
            CheckOutDate = request.CheckOutDate,
            Adults = request.Adults,
            Children = request.Children,
            RoomsCount = request.Rooms,
            Status = booking.Status,
            Currency = room.Currency,
            SubTotal = pricing.SubTotal,
            UpgradeTotal = pricing.UpgradeTotal,
            DiscountAmount = pricing.DiscountAmount + pricing.PointsDiscount,
            PointsRedeemed = pricing.PointsRedeemed,
            TaxAmount = pricing.TaxAmount,
            TotalAmount = pricing.TotalAmount,
            CancellationDeadline = booking.CancellationDeadline,
            ConfirmedAt = booking.ConfirmedAt,
            CancelledAt = booking.CancelledAt,
            CreatedAt = booking.CreatedAt,
            SpecialRequests = booking.SpecialRequests,
            Guests = booking.Guests,
            Rooms = [roomData],
            Upgrades = upgradeData
        };

        await _bookingRepository.UpdateAsync(updated, cancellationToken);
        await _bookingRepository.ReplaceUpgradesAsync(booking.Id, upgradeModels, cancellationToken);
        await _bookingRepository.AddStatusHistoryAsync(
            booking.Id,
            booking.Status,
            booking.Status,
            "Booking modified",
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var refreshed = await _bookingRepository.GetByIdAsync(booking.Id, cancellationToken) ?? updated;

        return Result.Success(new ModifyBookingResponse
        {
            Booking = BookingDto.From(refreshed)
        });
    }
}
