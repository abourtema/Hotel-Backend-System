using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Abstractions.Services;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Application.UseCases.Bookings;
using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.UseCases.Upgrades;

public sealed class AddBookingUpgradeUseCase : IAddBookingUpgradeUseCase
{
    private readonly IAccessContext _accessContext;
    private readonly IBookingRepository _bookingRepository;
    private readonly IUpgradeRepository _upgradeRepository;
    private readonly IPricingService _pricingService;
    private readonly IUnitOfWork _unitOfWork;

    public AddBookingUpgradeUseCase(
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

    public async Task<Result<AddBookingUpgradeResponse>> ExecuteAsync(
        AddBookingUpgradeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_accessContext.IsAuthenticated || _accessContext.UserId is null)
        {
            return Result.Failure<AddBookingUpgradeResponse>(BusinessErrors.AuthenticationRequired());
        }

        if (request.Quantity <= 0)
        {
            return Result.Failure<AddBookingUpgradeResponse>(
                BusinessErrors.Validation("InvalidQuantity", "Upgrade quantity must be greater than zero."));
        }

        var booking = await _bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);
        if (booking is null)
        {
            return Result.Failure<AddBookingUpgradeResponse>(BusinessErrors.BookingNotFound(request.BookingId));
        }

        if (booking.UserId != _accessContext.UserId)
        {
            return Result.Failure<AddBookingUpgradeResponse>(BusinessErrors.UnauthorizedBookingAccess());
        }

        if (booking.Status is not (BookingStatus.Pending or BookingStatus.Confirmed))
        {
            return Result.Failure<AddBookingUpgradeResponse>(BusinessErrors.BookingNotModifiable());
        }

        var upgrade = await _upgradeRepository.GetByIdAsync(request.UpgradeOptionId, cancellationToken);
        if (upgrade is null)
        {
            return Result.Failure<AddBookingUpgradeResponse>(BusinessErrors.UpgradeNotFound(request.UpgradeOptionId));
        }

        var room = booking.Rooms.FirstOrDefault();
        if (room is null)
        {
            return Result.Failure<AddBookingUpgradeResponse>(BusinessErrors.BookingNotModifiable());
        }

        var upgradeModels = booking.Upgrades
            .Select(u => new CreateBookingUpgradeModel
            {
                UpgradeOptionId = u.UpgradeOptionId,
                Quantity = u.Quantity,
                UnitPrice = u.UnitPrice,
                TotalPrice = u.TotalPrice
            })
            .ToList();

        var existing = upgradeModels.FirstOrDefault(u => u.UpgradeOptionId == upgrade.Id);
        if (existing is not null)
        {
            var index = upgradeModels.IndexOf(existing);
            var newQty = existing.Quantity + request.Quantity;
            upgradeModels[index] = new CreateBookingUpgradeModel
            {
                UpgradeOptionId = upgrade.Id,
                Quantity = newQty,
                UnitPrice = upgrade.Price,
                TotalPrice = upgrade.Price * newQty
            };
        }
        else
        {
            upgradeModels.Add(new CreateBookingUpgradeModel
            {
                UpgradeOptionId = upgrade.Id,
                Quantity = request.Quantity,
                UnitPrice = upgrade.Price,
                TotalPrice = upgrade.Price * request.Quantity
            });
        }

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
            Upgrades = booking.Upgrades
        };

        await _bookingRepository.UpdateAsync(updated, cancellationToken);
        await _bookingRepository.ReplaceUpgradesAsync(booking.Id, upgradeModels, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var refreshed = await _bookingRepository.GetByIdAsync(booking.Id, cancellationToken) ?? updated;

        return Result.Success(new AddBookingUpgradeResponse
        {
            Booking = BookingDto.From(refreshed)
        });
    }
}
