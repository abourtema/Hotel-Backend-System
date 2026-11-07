using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Bookings;

public interface IGetBookingSummaryUseCase
{
    Task<Result<GetBookingSummaryResponse>> ExecuteAsync(
        GetBookingSummaryRequest request,
        CancellationToken cancellationToken = default);
}
