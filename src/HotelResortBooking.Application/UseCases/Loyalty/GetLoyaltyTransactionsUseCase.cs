using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Loyalty;

public sealed class GetLoyaltyTransactionsUseCase : IGetLoyaltyTransactionsUseCase
{
    private readonly IAccessContext _accessContext;
    private readonly ILoyaltyRepository _loyaltyRepository;

    public GetLoyaltyTransactionsUseCase(IAccessContext accessContext, ILoyaltyRepository loyaltyRepository)
    {
        _accessContext = accessContext;
        _loyaltyRepository = loyaltyRepository;
    }

    public async Task<Result<GetLoyaltyTransactionsResponse>> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        if (!_accessContext.IsAuthenticated || _accessContext.UserId is null)
        {
            return Result.Failure<GetLoyaltyTransactionsResponse>(BusinessErrors.AuthenticationRequired());
        }

        var transactions = await _loyaltyRepository.GetTransactionsAsync(
            _accessContext.UserId.Value,
            cancellationToken);

        return Result.Success(new GetLoyaltyTransactionsResponse
        {
            Transactions = transactions
        });
    }
}
