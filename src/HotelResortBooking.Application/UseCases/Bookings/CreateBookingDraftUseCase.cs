using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Abstractions.Services;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Domain.Rules;

namespace HotelResortBooking.Application.UseCases.Bookings;

public sealed class CreateBookingDraftUseCase : ICreateBookingDraftUseCase
{
    private readonly IPropertyRepository _propertyRepository;
    private readonly IRoomTypeRepository _roomTypeRepository;
    private readonly IUpgradeRepository _upgradeRepository;
    private readonly IRoomAvailabilityService _availabilityService;
    private readonly IPricingService _pricingService;

    public CreateBookingDraftUseCase(
        IPropertyRepository propertyRepository,
        IRoomTypeRepository roomTypeRepository,
        IUpgradeRepository upgradeRepository,
        IRoomAvailabilityService availabilityService,
        IPricingService pricingService)
    {
        _propertyRepository = propertyRepository;
        _roomTypeRepository = roomTypeRepository;
        _upgradeRepository = upgradeRepository;
        _availabilityService = availabilityService;
        _pricingService = pricingService;
    }

    public async Task<Result<CreateBookingDraftResponse>> ExecuteAsync(
        CreateBookingDraftRequest request,
        CancellationToken cancellationToken = default)
    {
        if (new DateRangeMustBeValidRule(request.CheckInDate, request.CheckOutDate).IsBroken())
        {
            return Result.Failure<CreateBookingDraftResponse>(BusinessErrors.InvalidDateRange());
        }

        if (new GuestCountMustBeValidRule(request.Adults, request.Children).IsBroken())
        {
            return Result.Failure<CreateBookingDraftResponse>(BusinessErrors.InvalidGuestCount());
        }

        if (new RoomCountMustBeValidRule(request.Rooms).IsBroken())
        {
            return Result.Failure<CreateBookingDraftResponse>(BusinessErrors.InvalidRoomCount());
        }

        if (new LoyaltyPointsMustBeNonNegativeRule(request.PointsToRedeem).IsBroken())
        {
            return Result.Failure<CreateBookingDraftResponse>(
                BusinessErrors.Validation("InvalidPoints", "Points to redeem cannot be negative."));
        }

        if (!await _propertyRepository.ExistsActiveAsync(request.PropertyId, cancellationToken))
        {
            return Result.Failure<CreateBookingDraftResponse>(BusinessErrors.PropertyNotFound(request.PropertyId));
        }

        var details = await _propertyRepository.GetDetailsAsync(request.PropertyId, cancellationToken);
        if (details is null)
        {
            return Result.Failure<CreateBookingDraftResponse>(BusinessErrors.PropertyNotFound(request.PropertyId));
        }

        var room = await _roomTypeRepository.GetByIdAsync(request.RoomTypeId, cancellationToken);
        if (room is null || room.PropertyId != request.PropertyId)
        {
            return Result.Failure<CreateBookingDraftResponse>(BusinessErrors.RoomNotFound(request.RoomTypeId));
        }

        if (new OccupancyMustFitRoomRule(
                request.Adults,
                request.Children,
                request.Rooms,
                room.MaxAdults,
                room.MaxChildren,
                room.MaxGuests).IsBroken())
        {
            return Result.Failure<CreateBookingDraftResponse>(BusinessErrors.InvalidOccupancy());
        }

        var availability = await _availabilityService.CheckAsync(new AvailabilityCheckRequest
        {
            RoomTypeId = request.RoomTypeId,
            CheckInDate = request.CheckInDate,
            CheckOutDate = request.CheckOutDate,
            RoomsRequested = request.Rooms
        }, cancellationToken);

        if (!availability.IsAvailable)
        {
            return Result.Failure<CreateBookingDraftResponse>(BusinessErrors.RoomUnavailable());
        }

        var upgradeLines = new List<UpgradePriceLine>();
        var upgradeDtos = new List<BookingDraftUpgradeDto>();
        foreach (var selection in request.Upgrades)
        {
            if (selection.Quantity <= 0)
            {
                continue;
            }

            var upgrade = await _upgradeRepository.GetByIdAsync(selection.UpgradeOptionId, cancellationToken);
            if (upgrade is null)
            {
                return Result.Failure<CreateBookingDraftResponse>(BusinessErrors.UpgradeNotFound(selection.UpgradeOptionId));
            }

            upgradeLines.Add(new UpgradePriceLine
            {
                UpgradeOptionId = upgrade.Id,
                UnitPrice = upgrade.Price,
                Quantity = selection.Quantity
            });

            upgradeDtos.Add(new BookingDraftUpgradeDto
            {
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
            PointsToRedeem = request.PointsToRedeem,
            Currency = room.Currency
        });

        return Result.Success(new CreateBookingDraftResponse
        {
            PropertyId = request.PropertyId,
            PropertyName = details.Name,
            RoomTypeId = room.RoomTypeId,
            RoomTypeName = room.Name,
            CheckInDate = request.CheckInDate,
            CheckOutDate = request.CheckOutDate,
            Adults = request.Adults,
            Children = request.Children,
            RoomsCount = request.Rooms,
            Nights = nights,
            Currency = room.Currency,
            PricePerNight = room.PricePerNight,
            SubTotal = pricing.SubTotal,
            UpgradeTotal = pricing.UpgradeTotal,
            DiscountAmount = pricing.DiscountAmount,
            PointsRedeemed = pricing.PointsRedeemed,
            PointsDiscount = pricing.PointsDiscount,
            TaxAmount = pricing.TaxAmount,
            TotalAmount = pricing.TotalAmount,
            RequiresAuthenticationToFinalize = true,
            PriceBreakdown = BookingPriceBreakdown.From(pricing),
            Upgrades = upgradeDtos
        });
    }
}
