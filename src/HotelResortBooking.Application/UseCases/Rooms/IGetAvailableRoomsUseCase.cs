using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Rooms;

public interface IGetAvailableRoomsUseCase
{
    Task<Result<GetAvailableRoomsResponse>> ExecuteAsync(
        GetAvailableRoomsRequest request,
        CancellationToken cancellationToken = default);
}
