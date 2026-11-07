using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Abstractions.Services;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Application.UseCases.Bookings;
using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.UseCases.Upgrades;

public sealed class RemoveBookingUpgradeUseCase : IRemoveBookingUpgradeUseCase
{
    private readonly IAccessContext _accessContext;
    private readonly IBookingRepository _bookingRepository;
    private readonly IUpgradeRepository _upgradeRepository;
    private readonly IPricingService _pricingService;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveBookingUpgradeUseCase(
        IAccessContext accessContext,
        IBookingRepository bookingRepository,
        IUpgradeRepository upgradeRepository,
        IPricingService pricingService,
        IUnitOfWork unitOfWork)
    {
        _accessContext = accessContext;
        _bookingRepository = bookingRepository;
        _upgradeRepository = upgradeRepository;
        _pricingService = pricingService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<RemoveBookingUpgradeResponse>> ExecuteAsync(
        RemoveBookingUpgradeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_accessContext.IsAuthenticated || _accessContext.UserId is null)
        {
            return Result.Failure<RemoveBookingUpgradeResponse>(BusinessErrors.AuthenticationRequired());
        }

        var booking = await _bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);
        if (booking is null)
        {
            return Result.Failure<RemoveBookingUpgradeResponse>(BusinessErrors.BookingNotFound(request.BookingId));
        }

        if (booking.UserId != _accessContext.UserId)
        {
            return Result.Failure<RemoveBookingUpgradeResponse>(BusinessErrors.UnauthorizedBookingAccess());
        }

        if (booking.Status is not (BookingStatus.Pending or BookingStatus.Confirmed))
        {
            return Result.Failure<RemoveBookingUpgradeResponse>(BusinessErrors.BookingNotModifiable());
        }

        var existing = booking.Upgrades.FirstOrDefault(u => u.UpgradeOptionId == request.UpgradeOptionId);
        if (existing is null)
        {
            return Result.Failure<RemoveBookingUpgradeResponse>(BusinessErrors.UpgradeNotFound(request.UpgradeOptionId));
        }

        if (existing.IsMandatory)
        {
            return Result.Failure<RemoveBookingUpgradeResponse>(BusinessErrors.UpgradeNotRemovable());
        }

        // Confirm option still exists / active for business rule messaging.
        var upgradeOption = await _upgradeRepository.GetByIdAsync(request.UpgradeOptionId, cancellationToken);
        if (upgradeOption is not null && upgradeOption.IsMandatory)
        {
            return Result.Failure<RemoveBookingUpgradeResponse>(BusinessErrors.UpgradeNotRemovable());
        }

        var room = booking.Rooms.FirstOrDefault();
        if (room is null)
        {
            return Result.Failure<RemoveBookingUpgradeResponse>(BusinessErrors.BookingNotModifiable());
        }

        var upgradeModels = booking.Upgrades
            .Where(u => u.UpgradeOptionId != request.UpgradeOptionId)
            .Select(u => new CreateBookingUpgradeModel
            {
                UpgradeOptionId = u.UpgradeOptionId,
                Quantity = u.Quantity,
                UnitPrice = u.UnitPrice,
                TotalPrice = u.TotalPrice
            })
            .ToList();

        var pricing = _pricingService.Calculate(new PricingRequest
        {
            RoomPricePerNight = room.PricePerNight,
            Nights = room.Nights,
            RoomQuantity = room.Quantity,
            Upgrades = upgradeModels.Select(u => new UpgradePriceLine
            {
                UpgradeOptionId = u.UpgradeOptionId,
                UnitPrice = u.UnitPrice,
                Quantity = u.Quantity
            }).ToList(),
            PointsToRedeem = booking.PointsRedeemed,
            Currency = booking.Currency
        });

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
            Status = booking.Status,
            Currency = booking.Currency,
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
            Rooms = booking.Rooms,
            Upgrades = booking.Upgrades.Where(u => u.UpgradeOptionId != request.UpgradeOptionId).ToList()
        };

        await _bookingRepository.UpdateAsync(updated, cancellationToken);
        await _bookingRepository.ReplaceUpgradesAsync(booking.Id, upgradeModels, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var refreshed = await _bookingRepository.GetByIdAsync(booking.Id, cancellationToken) ?? updated;

        return Result.Success(new RemoveBookingUpgradeResponse
        {
            Booking = BookingDto.From(refreshed)
        });
    }
}
