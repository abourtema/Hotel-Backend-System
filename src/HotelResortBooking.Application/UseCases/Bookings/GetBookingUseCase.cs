using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Bookings;

public sealed class GetBookingUseCase : IGetBookingUseCase
{
    private readonly IAccessContext _accessContext;
    private readonly IBookingRepository _bookingRepository;
    private readonly IPaymentRepository _paymentRepository;

    public GetBookingUseCase(
        IAccessContext accessContext,
        IBookingRepository bookingRepository,
        IPaymentRepository paymentRepository)
    {
        _accessContext = accessContext;
        _bookingRepository = bookingRepository;
        _paymentRepository = paymentRepository;
    }

    public async Task<Result<GetBookingResponse>> ExecuteAsync(
        GetBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_accessContext.IsAuthenticated || _accessContext.UserId is null)
        {
            return Result.Failure<GetBookingResponse>(BusinessErrors.AuthenticationRequired());
        }

        BookingData? booking = null;
        if (request.BookingId is not null)
        {
            booking = await _bookingRepository.GetByIdAsync(request.BookingId.Value, cancellationToken);
            if (booking is null)
            {
                return Result.Failure<GetBookingResponse>(BusinessErrors.BookingNotFound(request.BookingId.Value));
            }
        }
        else if (!string.IsNullOrWhiteSpace(request.BookingNumber))
        {
            booking = await _bookingRepository.GetByBookingNumberAsync(request.BookingNumber.Trim(), cancellationToken);
            if (booking is null)
            {
                return Result.Failure<GetBookingResponse>(BusinessErrors.BookingNumberNotFound(request.BookingNumber.Trim()));
            }
        }
        else
        {
            return Result.Failure<GetBookingResponse>(
                BusinessErrors.Validation("InvalidBookingLookup", "Booking id or booking number is required."));
        }

        if (booking.UserId != _accessContext.UserId)
        {
            return Result.Failure<GetBookingResponse>(BusinessErrors.UnauthorizedBookingAccess());
        }

        var payments = await _paymentRepository.GetByBookingIdAsync(booking.Id, cancellationToken);
        return Result.Success(new GetBookingResponse
        {
            Booking = BookingDto.From(booking, BookingDto.SelectDisplayPayment(payments))
        });
    }
}
