namespace HotelResortBooking.Application.Features.Management.Dashboard;

public sealed class GetManagementDashboardQuery
{
    public DateTimeOffset? DateFrom { get; init; }

    public DateTimeOffset? DateTo { get; init; }
}

public sealed class ManagementDashboardResponse
{
    public int TotalHotels { get; init; }

    public int ActiveHotels { get; init; }

    public int TotalRooms { get; init; }

    public int AvailableRooms { get; init; }

    public int TotalBookings { get; init; }

    public int PendingBookings { get; init; }

    public int ConfirmedBookings { get; init; }

    public int CancelledBookings { get; init; }

    public int CompletedBookings { get; init; }

    public int TotalCustomers { get; init; }

    public decimal TotalRevenue { get; init; }

    public int TotalPayments { get; init; }

    public int SuccessfulPayments { get; init; }

    public int FailedPayments { get; init; }

    public int CurrentLoyaltyPoints { get; init; }

    public int ReviewCount { get; init; }
}

public interface IGetManagementDashboardQuery
{
    Task<ManagementDashboardResponse> ExecuteAsync(
        GetManagementDashboardQuery query,
        CancellationToken cancellationToken = default);
}
