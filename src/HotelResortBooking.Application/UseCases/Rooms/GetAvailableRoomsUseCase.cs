using HotelResortBooking.Application.Abstractions.Repositories;
using HotelResortBooking.Application.Abstractions.Services;
using HotelResortBooking.Application.Common.Errors;
using HotelResortBooking.Application.Common.Results;
using HotelResortBooking.Domain.Rules;

namespace HotelResortBooking.Application.UseCases.Rooms;

public sealed class GetAvailableRoomsUseCase : IGetAvailableRoomsUseCase
{
    private readonly IPropertyRepository _propertyRepository;
    private readonly IRoomTypeRepository _roomTypeRepository;
    private readonly IPricingService _pricingService;

    public GetAvailableRoomsUseCase(
        IPropertyRepository propertyRepository,
        IRoomTypeRepository roomTypeRepository,
        IPricingService pricingService)
    {
        _propertyRepository = propertyRepository;
        _roomTypeRepository = roomTypeRepository;
        _pricingService = pricingService;
    }

    public async Task<Result<GetAvailableRoomsResponse>> ExecuteAsync(
        GetAvailableRoomsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (new DateRangeMustBeValidRule(request.CheckInDate, request.CheckOutDate).IsBroken())
        {
            return Result.Failure<GetAvailableRoomsResponse>(BusinessErrors.InvalidDateRange());
        }

        if (new GuestCountMustBeValidRule(request.Adults, request.Children).IsBroken())
        {
            return Result.Failure<GetAvailableRoomsResponse>(BusinessErrors.InvalidGuestCount());
        }

        if (new RoomCountMustBeValidRule(request.Rooms).IsBroken())
        {
            return Result.Failure<GetAvailableRoomsResponse>(BusinessErrors.InvalidRoomCount());
        }

        if (!await _propertyRepository.ExistsActiveAsync(request.PropertyId, cancellationToken))
        {
            var details = await _propertyRepository.GetDetailsAsync(request.PropertyId, cancellationToken);
            if (details is null)
            {
                return Result.Failure<GetAvailableRoomsResponse>(BusinessErrors.PropertyNotFound(request.PropertyId));
            }

            return Result.Failure<GetAvailableRoomsResponse>(BusinessErrors.PropertyInactive(request.PropertyId));
        }

        var nights = _pricingService.CalculateNights(request.CheckInDate, request.CheckOutDate);
        var available = await _roomTypeRepository.GetAvailableRoomsAsync(
            request.PropertyId,
            request.CheckInDate,
            request.CheckOutDate,
            request.Rooms,
            cancellationToken);

        var options = new List<AvailableRoomOptionDto>();
        foreach (var room in available)
        {
            if (new OccupancyMustFitRoomRule(
                    request.Adults,
                    request.Children,
                    request.Rooms,
                    room.MaxAdults,
                    room.MaxChildren,
                    room.MaxGuests).IsBroken())
            {
                continue;
            }

            var pricing = _pricingService.Calculate(new PricingRequest
            {
                RoomPricePerNight = room.PricePerNight,
                Nights = nights,
                RoomQuantity = request.Rooms,
                Currency = room.Currency
            });

            options.Add(new AvailableRoomOptionDto
            {
                RoomTypeId = room.RoomTypeId,
                Name = room.Name,
                Description = room.Description,
                MaxAdults = room.MaxAdults,
                MaxChildren = room.MaxChildren,
                MaxGuests = room.MaxGuests,
                BedType = room.BedType,
                PricePerNight = room.PricePerNight,
                TotalPrice = pricing.TotalAmount,
                Currency = room.Currency,
                AvailableCount = room.AvailableCount,
                Images = room.Images,
                UpgradeOptions = room.UpgradeOptions
            });
        }

        return Result.Success(new GetAvailableRoomsResponse
        {
            PropertyId = request.PropertyId,
            CheckInDate = request.CheckInDate,
            CheckOutDate = request.CheckOutDate,
            Nights = nights,
            Rooms = options
        });
    }
}
