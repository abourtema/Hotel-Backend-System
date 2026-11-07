namespace HotelResortBooking.Application.Common;

/// <summary>
/// Marker interface for requests that return a response.
/// </summary>
/// <typeparam name="TResponse">Response type.</typeparam>
public interface IRequest<out TResponse> : IRequest
{
}
