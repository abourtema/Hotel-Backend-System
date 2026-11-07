using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Abstractions.Services;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Paging;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Application.UseCases.Payments;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.Domain.Management;

namespace HotelResortBooking.Application.Features.Management.Payments;

public sealed class GetManagedPaymentsQuery
{
    public Guid? BookingId { get; init; }

    public Guid? CustomerId { get; init; }

    public PaymentStatus? Status { get; init; }

    public PaymentMethodType? PaymentMethod { get; init; }

    public DateTimeOffset? DateFrom { get; init; }

    public DateTimeOffset? DateTo { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

public sealed class ManagedPaymentResponse
{
    public Guid PaymentId { get; init; }

    public Guid BookingId { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; } = string.Empty;

    public PaymentStatus Status { get; init; }

    public PaymentMethodType PaymentMethod { get; init; }

    public string? CardBrand { get; init; }

    public string? Last4Digits { get; init; }

    public string? ProviderTransactionId { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public DateTimeOffset? PaidAt { get; init; }

    public static ManagedPaymentResponse From(ManagedPaymentListItem item) => new()
    {
        PaymentId = item.Id,
        BookingId = item.BookingId,
        Amount = item.Amount,
        Currency = item.Currency,
        Status = item.Status,
        PaymentMethod = item.PaymentMethod,
        CardBrand = item.CardBrand,
        Last4Digits = item.CardLast4,
        ProviderTransactionId = item.ProviderTransactionId,
        CreatedAt = item.CreatedAt,
        PaidAt = item.PaidAt
    };

    public static ManagedPaymentResponse From(PaymentData payment) => new()
    {
        PaymentId = payment.Id,
        BookingId = payment.BookingId,
        Amount = payment.Amount,
        Currency = payment.Currency,
        Status = payment.Status,
        PaymentMethod = payment.PaymentMethod,
        CardBrand = payment.CardBrand,
        Last4Digits = payment.CardLast4,
        ProviderTransactionId = payment.ProviderTransactionId,
        CreatedAt = payment.CreatedAt,
        PaidAt = payment.PaidAt
    };
}

public interface IPaymentManagementCommands
{
    Task<Result<PagedResult<ManagedPaymentResponse>>> ListAsync(
        GetManagedPaymentsQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<ManagedPaymentResponse>> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<ManagedPaymentResponse>> ConfirmAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<ManagedPaymentResponse>> RefundAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed class PaymentManagementHandlers : IPaymentManagementCommands
{
    private readonly IManagementQueryRepository _queries;
    private readonly IPaymentRepository _payments;
    private readonly IConfirmPayAtHotelPaymentUseCase _confirmPayAtHotel;
    private readonly IPaymentGateway _gateway;
    private readonly IDateTimeProvider _clock;
    private readonly IAuditLogger _audit;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentManagementHandlers(
        IManagementQueryRepository queries,
        IPaymentRepository payments,
        IConfirmPayAtHotelPaymentUseCase confirmPayAtHotel,
        IPaymentGateway gateway,
        IDateTimeProvider clock,
        IAuditLogger audit,
        IUnitOfWork unitOfWork)
    {
        _queries = queries;
        _payments = payments;
        _confirmPayAtHotel = confirmPayAtHotel;
        _gateway = gateway;
        _clock = clock;
        _audit = audit;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PagedResult<ManagedPaymentResponse>>> ListAsync(
        GetManagedPaymentsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page <= 0 || query.PageSize <= 0)
        {
            return Result.Failure<PagedResult<ManagedPaymentResponse>>(BusinessErrors.InvalidPagination());
        }

        var page = await _queries.SearchPaymentsAsync(
            query.BookingId,
            query.CustomerId,
            query.Status,
            query.PaymentMethod,
            query.DateFrom,
            query.DateTo,
            query.Page,
            Math.Min(query.PageSize, 100),
            cancellationToken);

        return Result.Success(new PagedResult<ManagedPaymentResponse>
        {
            Items = page.Items.Select(ManagedPaymentResponse.From).ToList(),
            Page = page.Page,
            PageSize = page.PageSize,
            TotalCount = page.TotalCount
        });
    }

    public async Task<Result<ManagedPaymentResponse>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var payment = await _payments.GetByIdAsync(id, cancellationToken);
        return payment is null
            ? Result.Failure<ManagedPaymentResponse>(BusinessErrors.PaymentNotFound(id))
            : Result.Success(ManagedPaymentResponse.From(payment));
    }

    public async Task<Result<ManagedPaymentResponse>> ConfirmAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await _confirmPayAtHotel.ExecuteAsync(
            new ConfirmPayAtHotelPaymentRequest { PaymentId = id },
            cancellationToken);

        if (result.IsFailure)
        {
            return Result.Failure<ManagedPaymentResponse>(result.Error!);
        }

        var payment = await _payments.GetByIdAsync(id, cancellationToken);
        if (payment is null)
        {
            return Result.Failure<ManagedPaymentResponse>(BusinessErrors.PaymentNotFound(id));
        }

        await _audit.LogAsync(new AuditLogEntry
        {
            Action = AuditActions.PaymentConfirmed,
            EntityType = "Payment",
            EntityId = id.ToString(),
            NewValues = new
            {
                payment.Status,
                payment.PaymentMethod,
                payment.PaidAt,
                BookingStatus = result.Value.BookingStatus
            }
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ManagedPaymentResponse.From(payment));
    }

    public async Task<Result<ManagedPaymentResponse>> RefundAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var payment = await _payments.GetByIdAsync(id, cancellationToken);
        if (payment is null)
        {
            return Result.Failure<ManagedPaymentResponse>(BusinessErrors.PaymentNotFound(id));
        }

        if (payment.Status is not PaymentStatus.Succeeded)
        {
            return Result.Failure<ManagedPaymentResponse>(BusinessErrors.PaymentNotRefundable());
        }

        var refund = await _gateway.RefundAsync(
            new PaymentGatewayRefundRequest
            {
                PaymentId = payment.Id,
                ProviderTransactionId = payment.ProviderTransactionId,
                Amount = payment.Amount,
                Currency = payment.Currency
            },
            cancellationToken);

        if (!refund.Succeeded)
        {
            return Result.Failure<ManagedPaymentResponse>(
                BusinessErrors.Business("PaymentRefundFailed", refund.FailureReason ?? "Payment refund failed."));
        }

        var updated = new PaymentData
        {
            Id = payment.Id,
            BookingId = payment.BookingId,
            UserId = payment.UserId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            PaymentMethod = payment.PaymentMethod,
            Status = PaymentStatus.Refunded,
            Provider = payment.Provider,
            ProviderTransactionId = payment.ProviderTransactionId,
            PaymentMethodToken = payment.PaymentMethodToken,
            CardBrand = payment.CardBrand,
            CardLast4 = payment.CardLast4,
            FailureReason = payment.FailureReason,
            PaidAt = payment.PaidAt,
            CreatedAt = payment.CreatedAt
        };

        await _payments.UpdateAsync(updated, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _audit.LogAsync(new AuditLogEntry
        {
            Action = AuditActions.PaymentRefunded,
            EntityType = "Payment",
            EntityId = id.ToString(),
            OldValues = new { payment.Status, payment.Amount, payment.Currency },
            NewValues = new { Status = PaymentStatus.Refunded, refund.ProviderRefundId, RefundedAt = _clock.UtcNow }
        }, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(ManagedPaymentResponse.From(updated));
    }
}
