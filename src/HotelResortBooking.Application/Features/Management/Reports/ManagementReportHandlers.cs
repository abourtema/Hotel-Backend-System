using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Domain.Enums;

namespace HotelResortBooking.Application.Features.Management.Reports;

public sealed class ReportFilter
{
    public DateTimeOffset? DateFrom { get; init; }

    public DateTimeOffset? DateTo { get; init; }

    public Guid? PropertyId { get; init; }

    public BookingStatus? BookingStatus { get; init; }

    public PaymentStatus? PaymentStatus { get; init; }
}

public sealed class BookingsReportResponse
{
    public IReadOnlyList<BookingsReportRow> Items { get; init; } = Array.Empty<BookingsReportRow>();
}

public sealed class RevenueReportResponse
{
    public IReadOnlyList<RevenueReportRow> Items { get; init; } = Array.Empty<RevenueReportRow>();
}

public sealed class OccupancyReportResponse
{
    public IReadOnlyList<OccupancyReportRow> Items { get; init; } = Array.Empty<OccupancyReportRow>();
}

public sealed class PaymentsReportResponse
{
    public IReadOnlyList<PaymentsReportRow> Items { get; init; } = Array.Empty<PaymentsReportRow>();
}

public interface IManagementReportQueries
{
    Task<Result<BookingsReportResponse>> GetBookingsAsync(ReportFilter filter, CancellationToken cancellationToken = default);

    Task<Result<RevenueReportResponse>> GetRevenueAsync(ReportFilter filter, CancellationToken cancellationToken = default);

    Task<Result<OccupancyReportResponse>> GetOccupancyAsync(ReportFilter filter, CancellationToken cancellationToken = default);

    Task<Result<PaymentsReportResponse>> GetPaymentsAsync(ReportFilter filter, CancellationToken cancellationToken = default);
}

public sealed class ManagementReportHandlers : IManagementReportQueries
{
    private readonly IManagementQueryRepository _queries;
    private readonly IDateTimeProvider _clock;

    public ManagementReportHandlers(IManagementQueryRepository queries, IDateTimeProvider clock)
    {
        _queries = queries;
        _clock = clock;
    }

    public async Task<Result<BookingsReportResponse>> GetBookingsAsync(
        ReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var items = await _queries.GetBookingsReportAsync(
            filter.DateFrom,
            filter.DateTo,
            filter.PropertyId,
            filter.BookingStatus,
            cancellationToken);
        return Result.Success(new BookingsReportResponse { Items = items });
    }

    public async Task<Result<RevenueReportResponse>> GetRevenueAsync(
        ReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var items = await _queries.GetRevenueReportAsync(
            filter.DateFrom,
            filter.DateTo,
            filter.PropertyId,
            cancellationToken);
        return Result.Success(new RevenueReportResponse { Items = items });
    }

    public async Task<Result<OccupancyReportResponse>> GetOccupancyAsync(
        ReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var to = filter.DateTo?.UtcDateTime is DateTime toUtc
            ? DateOnly.FromDateTime(toUtc)
            : DateOnly.FromDateTime(_clock.UtcNow.UtcDateTime);
        var from = filter.DateFrom?.UtcDateTime is DateTime fromUtc
            ? DateOnly.FromDateTime(fromUtc)
            : to.AddDays(-30);

        var items = await _queries.GetOccupancyReportAsync(from, to, filter.PropertyId, cancellationToken);
        return Result.Success(new OccupancyReportResponse { Items = items });
    }

    public async Task<Result<PaymentsReportResponse>> GetPaymentsAsync(
        ReportFilter filter,
        CancellationToken cancellationToken = default)
    {
        var items = await _queries.GetPaymentsReportAsync(
            filter.DateFrom,
            filter.DateTo,
            filter.PropertyId,
            filter.PaymentStatus,
            cancellationToken);
        return Result.Success(new PaymentsReportResponse { Items = items });
    }
}
