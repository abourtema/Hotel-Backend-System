using HotelResortBooking.Application.Abstractions;
using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;

namespace HotelResortBooking.Application.UseCases.Bookings;

public sealed class GetUserBookingsUseCase : IGetUserBookingsUseCase
{
    private readonly IAccessContext _accessContext;
    private readonly IBookingRepository _bookingRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetUserBookingsUseCase(
        IAccessContext accessContext,
        IBookingRepository bookingRepository,
        IPaymentRepository paymentRepository,
        IDateTimeProvider dateTimeProvider)
    {
        _accessContext = accessContext;
        _bookingRepository = bookingRepository;
        _paymentRepository = paymentRepository;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<GetUserBookingsResponse>> ExecuteAsync(
        GetUserBookingsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_accessContext.IsAuthenticated || _accessContext.UserId is null)
        {
            return Result.Failure<GetUserBookingsResponse>(BusinessErrors.AuthenticationRequired());
        }

        var today = DateOnly.FromDateTime(_dateTimeProvider.UtcNow.UtcDateTime);
        var userId = _accessContext.UserId.Value;

        if (request.Group is not null)
        {
            var filtered = await _bookingRepository.GetUserBookingsAsync(
                userId,
                request.Group,
                today,
                cancellationToken);

            var mapped = await MapWithPaymentsAsync(filtered, cancellationToken);
            return Result.Success(new GetUserBookingsResponse
            {
                Upcoming = request.Group == BookingListGroup.Upcoming ? mapped : Array.Empty<BookingDto>(),
                Ongoing = request.Group == BookingListGroup.Ongoing ? mapped : Array.Empty<BookingDto>(),
                Past = request.Group == BookingListGroup.Past ? mapped : Array.Empty<BookingDto>(),
                FilteredGroup = request.Group
            });
        }

        var upcoming = await _bookingRepository.GetUserBookingsAsync(
            userId, BookingListGroup.Upcoming, today, cancellationToken);
        var ongoing = await _bookingRepository.GetUserBookingsAsync(
            userId, BookingListGroup.Ongoing, today, cancellationToken);
        var past = await _bookingRepository.GetUserBookingsAsync(
            userId, BookingListGroup.Past, today, cancellationToken);

        var paymentsByBooking = await LoadDisplayPaymentsAsync(
            upcoming.Concat(ongoing).Concat(past).Select(b => b.Id),
            cancellationToken);

        return Result.Success(new GetUserBookingsResponse
        {
            Upcoming = MapBookings(upcoming, paymentsByBooking),
            Ongoing = MapBookings(ongoing, paymentsByBooking),
            Past = MapBookings(past, paymentsByBooking)
        });
    }

    private async Task<IReadOnlyList<BookingDto>> MapWithPaymentsAsync(
        IReadOnlyList<BookingData> bookings,
        CancellationToken cancellationToken)
    {
        var paymentsByBooking = await LoadDisplayPaymentsAsync(
            bookings.Select(b => b.Id),
            cancellationToken);
        return MapBookings(bookings, paymentsByBooking);
    }

    private async Task<IReadOnlyDictionary<Guid, PaymentData>> LoadDisplayPaymentsAsync(
        IEnumerable<Guid> bookingIds,
        CancellationToken cancellationToken)
    {
        var ids = bookingIds.Distinct().ToList();
        var payments = await _paymentRepository.GetByBookingIdsAsync(ids, cancellationToken);
        return BookingDto.IndexDisplayPayments(payments);
    }

    private static IReadOnlyList<BookingDto> MapBookings(
        IReadOnlyList<BookingData> bookings,
        IReadOnlyDictionary<Guid, PaymentData> paymentsByBooking)
        => bookings
            .Select(booking => BookingDto.From(
                booking,
                paymentsByBooking.GetValueOrDefault(booking.Id)))
            .ToList();
}
