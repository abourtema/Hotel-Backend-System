using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Paging;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Application.UseCases.Bookings;
using HotelResortBooking.Domain.Enums;
using HotelResortBooking.Domain.Management;

namespace HotelResortBooking.Application.Features.Management.Bookings;

public sealed class GetManagedBookingsQuery
{
    public string? BookingNumber { get; init; }

    public Guid? UserId { get; init; }

    public Guid? PropertyId { get; init; }

    public BookingStatus? Status { get; init; }

    public DateOnly? CheckInDate { get; init; }

    public DateOnly? CheckOutDate { get; init; }

    public PaymentStatus? PaymentStatus { get; init; }

    public PaymentMethodType? PaymentMethod { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

public interface IBookingManagementCommands
{
    Task<Result<PagedResult<ManagedBookingListItem>>> ListAsync(
        GetManagedBookingsQuery query,
        CancellationToken cancellationToken = default);

    Task<Result<BookingDto>> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Result<ModifyBookingResponse>> ModifyAsync(
        Guid id,
        ModifyBookingRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<CancelBookingResponse>> CancelAsync(
        Guid id,
        string? reason,
        CancellationToken cancellationToken = default);

    Task<Result<ConfirmBookingResponse>> ConfirmAsync(
        Guid id,
        string? reason,
        CancellationToken cancellationToken = default);

    Task<Result<CompleteBookingResponse>> CompleteAsync(
        Guid id,
        string? reason,
        CancellationToken cancellationToken = default);
}

public sealed class BookingManagementHandlers : IBookingManagementCommands
{
    private readonly IManagementQueryRepository _queries;
    private readonly IBookingRepository _bookings;
    private readonly IPaymentRepository _payments;
    private readonly IModifyBookingUseCase _modifyBooking;
    private readonly ICancelBookingUseCase _cancelBooking;
    private readonly IConfirmBookingUseCase _confirmBooking;
    private readonly ICompleteBookingUseCase _completeBooking;
    private readonly IAuditLogger _audit;
    private readonly IUnitOfWork _unitOfWork;

    public BookingManagementHandlers(
        IManagementQueryRepository queries,
        IBookingRepository bookings,
        IPaymentRepository payments,
        IModifyBookingUseCase modifyBooking,
        ICancelBookingUseCase cancelBooking,
        IConfirmBookingUseCase confirmBooking,
        ICompleteBookingUseCase completeBooking,
        IAuditLogger audit,
        IUnitOfWork unitOfWork)
    {
        _queries = queries;
        _bookings = bookings;
        _payments = payments;
        _modifyBooking = modifyBooking;
        _cancelBooking = cancelBooking;
        _confirmBooking = confirmBooking;
        _completeBooking = completeBooking;
        _audit = audit;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<PagedResult<ManagedBookingListItem>>> ListAsync(
        GetManagedBookingsQuery query,
        CancellationToken cancellationToken = default)
    {
        if (query.Page <= 0 || query.PageSize <= 0)
        {
            return Result.Failure<PagedResult<ManagedBookingListItem>>(BusinessErrors.InvalidPagination());
        }

        var page = await _queries.SearchBookingsAsync(
            query.BookingNumber,
            query.UserId,
            query.PropertyId,
            query.Status,
            query.CheckInDate,
            query.CheckOutDate,
            query.PaymentStatus,
            query.PaymentMethod,
            query.Page,
            Math.Min(query.PageSize, 100),
            cancellationToken);

        return Result.Success(page);
    }

    public async Task<Result<BookingDto>> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var booking = await _bookings.GetByIdAsync(id, cancellationToken);
        if (booking is null)
        {
            return Result.Failure<BookingDto>(BusinessErrors.BookingNotFound(id));
        }

        var payments = await _payments.GetByBookingIdAsync(id, cancellationToken);
        return Result.Success(BookingDto.From(booking, BookingDto.SelectDisplayPayment(payments)));
    }

    public async Task<Result<ModifyBookingResponse>> ModifyAsync(
        Guid id,
        ModifyBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await _bookings.GetByIdAsync(id, cancellationToken);
        var result = await _modifyBooking.ExecuteAsync(
            new ModifyBookingRequest
            {
                BookingId = id,
                CheckInDate = request.CheckInDate,
                CheckOutDate = request.CheckOutDate,
                Adults = request.Adults,
                Children = request.Children,
                Rooms = request.Rooms,
                RoomTypeId = request.RoomTypeId,
                Upgrades = request.Upgrades,
                BypassOwnershipCheck = true
            },
            cancellationToken);

        if (result.IsSuccess)
        {
            await _audit.LogAsync(new AuditLogEntry
            {
                Action = AuditActions.BookingModified,
                EntityType = "Booking",
                EntityId = id.ToString(),
                OldValues = existing is null ? null : BookingDto.From(existing),
                NewValues = result.Value.Booking
            }, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    public async Task<Result<CancelBookingResponse>> CancelAsync(
        Guid id,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var result = await _cancelBooking.ExecuteAsync(
            new CancelBookingRequest
            {
                BookingId = id,
                Reason = reason ?? "Cancelled by management",
                BypassOwnershipCheck = true
            },
            cancellationToken);

        if (result.IsSuccess)
        {
            await _audit.LogAsync(new AuditLogEntry
            {
                Action = AuditActions.BookingCancelled,
                EntityType = "Booking",
                EntityId = id.ToString(),
                NewValues = result.Value.Booking
            }, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    public async Task<Result<ConfirmBookingResponse>> ConfirmAsync(
        Guid id,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var result = await _confirmBooking.ExecuteAsync(
            new ConfirmBookingRequest
            {
                BookingId = id,
                Reason = reason,
                BypassOwnershipCheck = true
            },
            cancellationToken);

        if (result.IsSuccess)
        {
            await _audit.LogAsync(new AuditLogEntry
            {
                Action = AuditActions.BookingConfirmed,
                EntityType = "Booking",
                EntityId = id.ToString(),
                NewValues = result.Value.Booking
            }, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return result;
    }

    public async Task<Result<CompleteBookingResponse>> CompleteAsync(
        Guid id,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        var result = await _completeBooking.ExecuteAsync(
            new CompleteBookingRequest
            {
                BookingId = id,
                Reason = reason,
                BypassOwnershipCheck = true
            },
            cancellationToken);

        if (result.IsSuccess)
        {
            await _audit.LogAsync(new AuditLogEntry
            {
                Action = AuditActions.BookingCompleted,
                EntityType = "Booking",
                EntityId = id.ToString(),
                NewValues = result.Value.Booking
            }, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return result;
    }
}
