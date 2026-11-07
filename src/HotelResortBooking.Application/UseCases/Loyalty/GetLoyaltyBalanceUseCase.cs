using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Loyalty;

public sealed class GetLoyaltyBalanceUseCase : IGetLoyaltyBalanceUseCase
{
    private readonly IAccessContext _accessContext;
    private readonly ILoyaltyRepository _loyaltyRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GetLoyaltyBalanceUseCase(
        IAccessContext accessContext,
        ILoyaltyRepository loyaltyRepository,
        IUnitOfWork unitOfWork)
    {
        _accessContext = accessContext;
        _loyaltyRepository = loyaltyRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<GetLoyaltyBalanceResponse>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!_accessContext.IsAuthenticated || _accessContext.UserId is null)
        {
            return Result.Failure<GetLoyaltyBalanceResponse>(BusinessErrors.AuthenticationRequired());
        }

        var account = await _loyaltyRepository.GetOrCreateAccountAsync(
            _accessContext.UserId.Value,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new GetLoyaltyBalanceResponse
        {
            AccountId = account.Id,
            CurrentBalance = account.CurrentBalance,
            LifetimeEarned = account.LifetimeEarned,
            LifetimeRedeemed = account.LifetimeRedeemed
        });
    }
}
