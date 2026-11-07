using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Abstractions.Services;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Application.UseCases.Bookings;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.Domain.Rules;

namespace HotelResortBooking.Application.UseCases.Loyalty;

public sealed class ApplyLoyaltyPointsToBookingUseCase : IApplyLoyaltyPointsToBookingUseCase
{
    private readonly IAccessContext _accessContext;
    private readonly ILoyaltyRepository _loyaltyRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IPricingService _pricingService;
    private readonly IUnitOfWork _unitOfWork;

    public ApplyLoyaltyPointsToBookingUseCase(
        IAccessContext accessContext,
        ILoyaltyRepository loyaltyRepository,
        IBookingRepository bookingRepository,
        IPricingService pricingService,
        IUnitOfWork unitOfWork)
    {
        _accessContext = accessContext;
        _loyaltyRepository = loyaltyRepository;
        _bookingRepository = bookingRepository;
        _pricingService = pricingService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<ApplyLoyaltyPointsToBookingResponse>> ExecuteAsync(
        ApplyLoyaltyPointsToBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_accessContext.IsAuthenticated || _accessContext.UserId is null)
        {
            return Result.Failure<ApplyLoyaltyPointsToBookingResponse>(BusinessErrors.AuthenticationRequired());
        }

        if (new LoyaltyPointsMustBeNonNegativeRule(request.Points).IsBroken() || request.Points == 0)
        {
            return Result.Failure<ApplyLoyaltyPointsToBookingResponse>(
                BusinessErrors.Validation("InvalidPoints", "Points to apply must be greater than zero."));
        }

        var userId = _accessContext.UserId.Value;
        var booking = await _bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);
        if (booking is null)
        {
            return Result.Failure<ApplyLoyaltyPointsToBookingResponse>(BusinessErrors.BookingNotFound(request.BookingId));
        }

        if (booking.UserId != userId)
        {
            return Result.Failure<ApplyLoyaltyPointsToBookingResponse>(BusinessErrors.UnauthorizedBookingAccess());
        }

        if (booking.Status is not (BookingStatus.Pending or BookingStatus.Confirmed))
        {
            return Result.Failure<ApplyLoyaltyPointsToBookingResponse>(BusinessErrors.BookingNotModifiable());
        }

        var room = booking.Rooms.FirstOrDefault();
        if (room is null)
        {
            return Result.Failure<ApplyLoyaltyPointsToBookingResponse>(BusinessErrors.BookingNotModifiable());
        }

        var account = await _loyaltyRepository.GetOrCreateAccountAsync(userId, cancellationToken);
        if (account.CurrentBalance < request.Points)
        {
            return Result.Failure<ApplyLoyaltyPointsToBookingResponse>(BusinessErrors.InsufficientLoyaltyPoints());
        }

        var targetPoints = booking.PointsRedeemed + request.Points;
        var pricingWithoutCap = _pricingService.Calculate(new PricingRequest
        {
            RoomPricePerNight = room.PricePerNight,
            Nights = room.Nights,
            RoomQuantity = room.Quantity,
            Upgrades = booking.Upgrades.Select(u => new UpgradePriceLine
            {
                UpgradeOptionId = u.UpgradeOptionId,
                UnitPrice = u.UnitPrice,
                Quantity = u.Quantity
            }).ToList(),
            PointsToRedeem = 0,
            Currency = booking.Currency
        });

        var maxRedeemableValue = pricingWithoutCap.SubTotal + pricingWithoutCap.UpgradeTotal - pricingWithoutCap.DiscountAmount;
        if (_pricingService.CalculatePointsValue(targetPoints) > maxRedeemableValue)
        {
            return Result.Failure<ApplyLoyaltyPointsToBookingResponse>(BusinessErrors.PointsWouldReduceBelowZero());
        }

        var pricing = _pricingService.Calculate(new PricingRequest
        {
            RoomPricePerNight = room.PricePerNight,
            Nights = room.Nights,
            RoomQuantity = room.Quantity,
            Upgrades = booking.Upgrades.Select(u => new UpgradePriceLine
            {
                UpgradeOptionId = u.UpgradeOptionId,
                UnitPrice = u.UnitPrice,
                Quantity = u.Quantity
            }).ToList(),
            PointsToRedeem = targetPoints,
            Currency = booking.Currency
        });

        await _loyaltyRepository.RedeemAsync(
            userId,
            booking.Id,
            request.Points,
            "Loyalty points applied to booking",
            cancellationToken);

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
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var refreshed = await _bookingRepository.GetByIdAsync(booking.Id, cancellationToken) ?? updated;

        return Result.Success(new ApplyLoyaltyPointsToBookingResponse
        {
            Booking = BookingDto.From(refreshed),
            PointsApplied = request.Points,
            PointsDiscount = pricing.PointsDiscount
        });
    }
}
