using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Abstractions.Services;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Domain.Rules;

namespace HotelResortBooking.Application.UseCases.Loyalty;

public sealed class CalculatePointsRedemptionUseCase : ICalculatePointsRedemptionUseCase
{
    private readonly IAccessContext _accessContext;
    private readonly ILoyaltyRepository _loyaltyRepository;
    private readonly IPricingService _pricingService;

    public CalculatePointsRedemptionUseCase(
        IAccessContext accessContext,
        ILoyaltyRepository loyaltyRepository,
        IPricingService pricingService)
    {
        _accessContext = accessContext;
        _loyaltyRepository = loyaltyRepository;
        _pricingService = pricingService;
    }

    public async Task<Result<CalculatePointsRedemptionResponse>> ExecuteAsync(
        CalculatePointsRedemptionRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_accessContext.IsAuthenticated || _accessContext.UserId is null)
        {
            return Result.Failure<CalculatePointsRedemptionResponse>(BusinessErrors.AuthenticationRequired());
        }

        if (new LoyaltyPointsMustBeNonNegativeRule(request.Points).IsBroken())
        {
            return Result.Failure<CalculatePointsRedemptionResponse>(
                BusinessErrors.Validation("InvalidPoints", "Points cannot be negative."));
        }

        if (request.BookingTotalBeforePoints < 0)
        {
            return Result.Failure<CalculatePointsRedemptionResponse>(
                BusinessErrors.Validation("InvalidTotal", "Booking total cannot be negative."));
        }

        var account = await _loyaltyRepository.GetOrCreateAccountAsync(
            _accessContext.UserId.Value,
            cancellationToken);

        var applicablePoints = Math.Min(request.Points, account.CurrentBalance);
        var maxPointsByTotal = request.PointValue <= 0
            ? 0
            : (int)Math.Floor(request.BookingTotalBeforePoints / request.PointValue);
        applicablePoints = Math.Min(applicablePoints, maxPointsByTotal);

        var discount = _pricingService.CalculatePointsValue(applicablePoints, request.PointValue);
        if (discount > request.BookingTotalBeforePoints)
        {
            discount = request.BookingTotalBeforePoints;
        }

        return Result.Success(new CalculatePointsRedemptionResponse
        {
            RequestedPoints = request.Points,
            ApplicablePoints = applicablePoints,
            PointsDiscount = discount,
            RemainingPayable = Math.Max(0, request.BookingTotalBeforePoints - discount),
            AvailableBalance = account.CurrentBalance
        });
    }
}
