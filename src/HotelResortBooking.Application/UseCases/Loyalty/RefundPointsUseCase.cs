using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Abstractions.Services;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Domain.Rules;

namespace HotelResortBooking.Application.UseCases.Loyalty;

public sealed class RefundPointsUseCase : IRefundPointsUseCase
{
    private readonly IAccessContext _accessContext;
    private readonly ILoyaltyRepository _loyaltyRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IPricingService _pricingService;
    private readonly IUnitOfWork _unitOfWork;

    public RefundPointsUseCase(
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

    public async Task<Result<RefundPointsResponse>> ExecuteAsync(
        RefundPointsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_accessContext.IsAuthenticated || _accessContext.UserId is null)
        {
            return Result.Failure<RefundPointsResponse>(BusinessErrors.AuthenticationRequired());
        }

        if (new LoyaltyPointsMustBeNonNegativeRule(request.Points).IsBroken() || request.Points == 0)
        {
            return Result.Failure<RefundPointsResponse>(
                BusinessErrors.Validation("InvalidPoints", "Points to refund must be greater than zero."));
        }

        var userId = _accessContext.UserId.Value;
        var booking = await _bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);
        if (booking is null)
        {
            return Result.Failure<RefundPointsResponse>(BusinessErrors.BookingNotFound(request.BookingId));
        }

        if (booking.UserId != userId)
        {
            return Result.Failure<RefundPointsResponse>(BusinessErrors.UnauthorizedBookingAccess());
        }

        if (request.Points > booking.PointsRedeemed)
        {
            return Result.Failure<RefundPointsResponse>(
                BusinessErrors.Validation("InvalidRefund", "Cannot refund more points than were redeemed."));
        }

        var remainingPoints = booking.PointsRedeemed - request.Points;
        var room = booking.Rooms.FirstOrDefault();

        await _loyaltyRepository.RestoreAsync(
            userId,
            booking.Id,
            request.Points,
            request.Description ?? "Loyalty points refunded",
            cancellationToken);

        if (room is not null)
        {
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
                PointsToRedeem = remainingPoints,
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
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var account = await _loyaltyRepository.GetAccountByUserIdAsync(userId, cancellationToken);

        return Result.Success(new RefundPointsResponse
        {
            BookingId = booking.Id,
            PointsRefunded = request.Points,
            RemainingBalance = account?.CurrentBalance ?? 0
        });
    }
}
