using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Abstractions.Services;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.Domain.Rules;

namespace HotelResortBooking.Application.UseCases.Bookings;

public sealed class CreateBookingUseCase : ICreateBookingUseCase
{
    private readonly IAccessContext _accessContext;
    private readonly IPropertyRepository _propertyRepository;
    private readonly IRoomTypeRepository _roomTypeRepository;
    private readonly IUpgradeRepository _upgradeRepository;
    private readonly IRoomAvailabilityService _availabilityService;
    private readonly IPricingService _pricingService;
    private readonly IBookingRepository _bookingRepository;
    private readonly ILoyaltyRepository _loyaltyRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentGateway _paymentGateway;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IUnitOfWork _unitOfWork;

    public CreateBookingUseCase(
        IAccessContext accessContext,
        IPropertyRepository propertyRepository,
        IRoomTypeRepository roomTypeRepository,
        IUpgradeRepository upgradeRepository,
        IRoomAvailabilityService availabilityService,
        IPricingService pricingService,
        IBookingRepository bookingRepository,
        ILoyaltyRepository loyaltyRepository,
        IUserRepository userRepository,
        IPaymentRepository paymentRepository,
        IPaymentGateway paymentGateway,
        IDateTimeProvider dateTimeProvider,
        IUnitOfWork unitOfWork)
    {
        _accessContext = accessContext;
        _propertyRepository = propertyRepository;
        _roomTypeRepository = roomTypeRepository;
        _upgradeRepository = upgradeRepository;
        _availabilityService = availabilityService;
        _pricingService = pricingService;
        _bookingRepository = bookingRepository;
        _loyaltyRepository = loyaltyRepository;
        _userRepository = userRepository;
        _paymentRepository = paymentRepository;
        _paymentGateway = paymentGateway;
        _dateTimeProvider = dateTimeProvider;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<CreateBookingResponse>> ExecuteAsync(
        CreateBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_accessContext.IsAuthenticated || _accessContext.UserId is null)
        {
            return Result.Failure<CreateBookingResponse>(BusinessErrors.AuthenticationRequired());
        }

        var userId = _accessContext.UserId.Value;

        if (new DateRangeMustBeValidRule(request.CheckInDate, request.CheckOutDate).IsBroken())
        {
            return Result.Failure<CreateBookingResponse>(BusinessErrors.InvalidDateRange());
        }

        if (new GuestCountMustBeValidRule(request.Adults, request.Children).IsBroken())
        {
            return Result.Failure<CreateBookingResponse>(BusinessErrors.InvalidGuestCount());
        }

        if (new RoomCountMustBeValidRule(request.Rooms).IsBroken())
        {
            return Result.Failure<CreateBookingResponse>(BusinessErrors.InvalidRoomCount());
        }

        if (new LoyaltyPointsMustBeNonNegativeRule(request.PointsToRedeem).IsBroken())
        {
            return Result.Failure<CreateBookingResponse>(
                BusinessErrors.Validation("InvalidPoints", "Points to redeem cannot be negative."));
        }

        var guest = BookingCheckoutValidator.MergeWithProfile(
            request.Guest,
            await _userRepository.GetByIdAsync(userId, cancellationToken));

        var guestError = BookingCheckoutValidator.ValidateGuestDetails(
            guest,
            request.AdditionalGuests,
            request.Adults,
            request.Children,
            request.SpecialRequests);
        if (guestError is not null)
        {
            return Result.Failure<CreateBookingResponse>(guestError);
        }

        if (request.Payment is not null)
        {
            var paymentError = BookingCheckoutValidator.ValidateCheckoutPayment(request.Payment);
            if (paymentError is not null)
            {
                return Result.Failure<CreateBookingResponse>(paymentError);
            }
        }

        if (!await _propertyRepository.ExistsActiveAsync(request.PropertyId, cancellationToken))
        {
            return Result.Failure<CreateBookingResponse>(BusinessErrors.PropertyNotFound(request.PropertyId));
        }

        var room = await _roomTypeRepository.GetByIdAsync(request.RoomTypeId, cancellationToken);
        if (room is null || room.PropertyId != request.PropertyId)
        {
            return Result.Failure<CreateBookingResponse>(BusinessErrors.RoomNotFound(request.RoomTypeId));
        }

        if (new OccupancyMustFitRoomRule(
                request.Adults,
                request.Children,
                request.Rooms,
                room.MaxAdults,
                room.MaxChildren,
                room.MaxGuests).IsBroken())
        {
            return Result.Failure<CreateBookingResponse>(BusinessErrors.InvalidOccupancy());
        }

        var availabilityRequest = new AvailabilityCheckRequest
        {
            RoomTypeId = request.RoomTypeId,
            CheckInDate = request.CheckInDate,
            CheckOutDate = request.CheckOutDate,
            RoomsRequested = request.Rooms
        };

        var availability = await _availabilityService.CheckAsync(availabilityRequest, cancellationToken);
        if (!availability.IsAvailable)
        {
            return Result.Failure<CreateBookingResponse>(BusinessErrors.RoomUnavailable());
        }

        var upgradeModels = new List<CreateBookingUpgradeModel>();
        var upgradeLines = new List<UpgradePriceLine>();
        foreach (var selection in request.Upgrades)
        {
            if (selection.Quantity <= 0)
            {
                continue;
            }

            var upgrade = await _upgradeRepository.GetByIdAsync(selection.UpgradeOptionId, cancellationToken);
            if (upgrade is null)
            {
                return Result.Failure<CreateBookingResponse>(BusinessErrors.UpgradeNotFound(selection.UpgradeOptionId));
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
        }

        if (request.PointsToRedeem > 0)
        {
            var account = await _loyaltyRepository.GetOrCreateAccountAsync(userId, cancellationToken);
            if (account.CurrentBalance < request.PointsToRedeem)
            {
                return Result.Failure<CreateBookingResponse>(BusinessErrors.InsufficientLoyaltyPoints());
            }
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

        var reserved = await _availabilityService.ReserveAsync(availabilityRequest, cancellationToken);
        if (!reserved)
        {
            return Result.Failure<CreateBookingResponse>(BusinessErrors.RoomUnavailable());
        }

        var initialStatus = BookingCheckoutValidator.InitialBookingStatus(request.Payment);
        var confirmedAt = initialStatus == BookingStatus.Completed ? _dateTimeProvider.UtcNow : (DateTimeOffset?)null;

        var booking = await _bookingRepository.AddAsync(new CreateBookingModel
        {
            UserId = userId,
            PropertyId = request.PropertyId,
            CheckInDate = request.CheckInDate,
            CheckOutDate = request.CheckOutDate,
            Adults = request.Adults,
            Children = request.Children,
            RoomsCount = request.Rooms,
            Status = initialStatus,
            Currency = room.Currency,
            SubTotal = pricing.SubTotal,
            UpgradeTotal = pricing.UpgradeTotal,
            DiscountAmount = pricing.DiscountAmount + pricing.PointsDiscount,
            PointsRedeemed = pricing.PointsRedeemed,
            TaxAmount = pricing.TaxAmount,
            TotalAmount = pricing.TotalAmount,
            CancellationDeadline = _dateTimeProvider.UtcNow.AddDays(1),
            ConfirmedAt = confirmedAt,
            SpecialRequests = BookingCheckoutValidator.NormalizeSpecialRequests(request.SpecialRequests),
            Guests = BookingCheckoutValidator.BuildGuests(guest, request.AdditionalGuests),
            Rooms =
            [
                new CreateBookingRoomModel
                {
                    RoomTypeId = room.RoomTypeId,
                    Quantity = request.Rooms,
                    PricePerNight = room.PricePerNight,
                    Nights = nights,
                    SubTotal = pricing.SubTotal
                }
            ],
            Upgrades = upgradeModels
        }, cancellationToken);

        if (pricing.PointsRedeemed > 0)
        {
            await _loyaltyRepository.RedeemAsync(
                userId,
                booking.Id,
                pricing.PointsRedeemed,
                "Points applied at booking creation",
                cancellationToken);
        }

        PaymentData? payment = null;
        if (BookingCheckoutValidator.ShouldRecordOnlinePayment(request.Payment))
        {
            var paymentResult = await ApplyCheckoutPaymentAsync(
                booking,
                userId,
                request.Payment!,
                cancellationToken);
            if (paymentResult.IsFailure)
            {
                return Result.Failure<CreateBookingResponse>(paymentResult.Error!);
            }

            payment = paymentResult.Value;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateBookingResponse
        {
            Booking = BookingDto.From(booking, payment),
            PaymentId = payment?.Id,
            PaymentStatus = payment?.Status,
            PaymentMethod = payment?.PaymentMethod
        });
    }

    private async Task<Result<PaymentData>> ApplyCheckoutPaymentAsync(
        BookingData booking,
        Guid userId,
        BookingCheckoutPaymentInput paymentInput,
        CancellationToken cancellationToken)
    {
        var method = paymentInput.Method ?? PaymentMethodType.CreditCard;
        var idempotencyKey = string.IsNullOrWhiteSpace(paymentInput.IdempotencyKey)
            ? null
            : paymentInput.IdempotencyKey.Trim();
        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            var existingByKey = await _paymentRepository.GetByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
            if (existingByKey is not null)
            {
                return Result.Success(existingByKey);
            }
        }

        var hasToken = !string.IsNullOrWhiteSpace(paymentInput.PaymentMethodToken);
        var payment = await _paymentRepository.AddAsync(new CreatePaymentModel
        {
            BookingId = booking.Id,
            UserId = userId,
            Amount = booking.TotalAmount,
            Currency = booking.Currency,
            PaymentMethod = method,
            Provider = "stub",
            PaymentMethodToken = hasToken ? paymentInput.PaymentMethodToken!.Trim() : null,
            CardBrand = paymentInput.CardBrand,
            CardLast4 = paymentInput.CardLast4,
            IdempotencyKey = idempotencyKey
        }, cancellationToken);

        if (!hasToken || string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return Result.Success(payment);
        }

        var charge = await _paymentGateway.ChargeAsync(new PaymentGatewayChargeRequest
        {
            PaymentMethodToken = paymentInput.PaymentMethodToken!.Trim(),
            Amount = booking.TotalAmount,
            Currency = booking.Currency,
            IdempotencyKey = idempotencyKey,
            BookingId = booking.Id
        }, cancellationToken);

        var updated = new PaymentData
        {
            Id = payment.Id,
            BookingId = payment.BookingId,
            UserId = payment.UserId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            PaymentMethod = payment.PaymentMethod,
            Status = charge.Succeeded ? PaymentStatus.Succeeded : PaymentStatus.Failed,
            Provider = charge.Provider ?? payment.Provider,
            ProviderTransactionId = charge.ProviderTransactionId,
            PaymentMethodToken = payment.PaymentMethodToken,
            CardBrand = charge.CardBrand ?? payment.CardBrand,
            CardLast4 = charge.CardLast4 ?? payment.CardLast4,
            FailureReason = charge.Succeeded ? null : charge.FailureReason ?? "Payment failed",
            PaidAt = charge.Succeeded ? _dateTimeProvider.UtcNow : null,
            CreatedAt = payment.CreatedAt
        };

        await _paymentRepository.UpdateAsync(updated, cancellationToken);
        return Result.Success(updated);
    }
}
