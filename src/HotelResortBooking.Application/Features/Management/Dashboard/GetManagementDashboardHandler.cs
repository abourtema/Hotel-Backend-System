using HotelResortBooking.Application.Abstractions.Repositories;

namespace HotelResortBooking.Application.Features.Management.Dashboard;

public sealed class GetManagementDashboardHandler : IGetManagementDashboardQuery
{
    private readonly IManagementQueryRepository _queries;

    public GetManagementDashboardHandler(IManagementQueryRepository queries)
    {
        _queries = queries;
    }

    public async Task<ManagementDashboardResponse> ExecuteAsync(
        GetManagementDashboardQuery query,
        CancellationToken cancellationToken = default)
    {
        var totals = await _queries.GetDashboardAsync(query.DateFrom, query.DateTo, cancellationToken);
        return new ManagementDashboardResponse
        {
            TotalHotels = totals.TotalHotels,
            ActiveHotels = totals.ActiveHotels,
            TotalRooms = totals.TotalRooms,
            AvailableRooms = totals.AvailableRooms,
            TotalBookings = totals.TotalBookings,
            PendingBookings = totals.PendingBookings,
            ConfirmedBookings = totals.ConfirmedBookings,
            CancelledBookings = totals.CancelledBookings,
            CompletedBookings = totals.CompletedBookings,
            TotalCustomers = totals.TotalCustomers,
            TotalRevenue = totals.TotalRevenue,
            TotalPayments = totals.TotalPayments,
            SuccessfulPayments = totals.SuccessfulPayments,
            FailedPayments = totals.FailedPayments,
            CurrentLoyaltyPoints = totals.CurrentLoyaltyPoints,
            ReviewCount = totals.ReviewCount
        };
    }
}
