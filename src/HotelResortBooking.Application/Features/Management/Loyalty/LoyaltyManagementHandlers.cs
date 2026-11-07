using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Domain.Management;

namespace HotelResortBooking.Application.Features.Management.Loyalty;

public sealed class AdjustLoyaltyRequest
{
    public int PointsDelta { get; init; }

    public string? Reason { get; init; }
}

public interface ILoyaltyManagementCommands
{
    Task<Result<LoyaltyAccountData>> GetAccountAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<Result<IReadOnlyList<LoyaltyTransactionData>>> GetTransactionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<Result<LoyaltyAccountData>> AdjustAsync(
        Guid userId,
        AdjustLoyaltyRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class LoyaltyManagementHandlers : ILoyaltyManagementCommands
{
    private readonly ILoyaltyRepository _loyalty;
    private readonly IUserRepository _users;
    private readonly IAuditLogger _audit;
    private readonly IUnitOfWork _unitOfWork;

    public LoyaltyManagementHandlers(
        ILoyaltyRepository loyalty,
        IUserRepository users,
        IAuditLogger audit,
        IUnitOfWork unitOfWork)
    {
        _loyalty = loyalty;
        _users = users;
        _audit = audit;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<LoyaltyAccountData>> GetAccountAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<LoyaltyAccountData>(BusinessErrors.UserNotFound());
        }

        var account = await _loyalty.GetOrCreateAccountAsync(userId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(account);
    }

    public async Task<Result<IReadOnlyList<LoyaltyTransactionData>>> GetTransactionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<IReadOnlyList<LoyaltyTransactionData>>(BusinessErrors.UserNotFound());
        }

        var items = await _loyalty.GetTransactionsAsync(userId, cancellationToken);
        return Result.Success(items);
    }

    public async Task<Result<LoyaltyAccountData>> AdjustAsync(
        Guid userId,
        AdjustLoyaltyRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.PointsDelta == 0)
        {
            return Result.Failure<LoyaltyAccountData>(BusinessErrors.InvalidLoyaltyAdjustment());
        }

        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return Result.Failure<LoyaltyAccountData>(BusinessErrors.UserNotFound());
        }

        var before = await _loyalty.GetOrCreateAccountAsync(userId, cancellationToken);
        if (before.CurrentBalance + request.PointsDelta < 0)
        {
            return Result.Failure<LoyaltyAccountData>(BusinessErrors.InvalidLoyaltyAdjustment());
        }

        var after = await _loyalty.AdjustAsync(
            userId,
            request.PointsDelta,
            request.Reason ?? "Manual management adjustment",
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogEntry
        {
            Action = AuditActions.LoyaltyPointsAdjusted,
            EntityType = "LoyaltyAccount",
            EntityId = after.Id.ToString(),
            OldValues = new { before.CurrentBalance },
            NewValues = new { after.CurrentBalance, request.PointsDelta, request.Reason }
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success(after);
    }
}
