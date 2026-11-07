using HotelResortBooking.Domain.Rules;

namespace HotelResortBooking.Domain.Rules;

public sealed class DateRangeMustBeValidRule : IBusinessRule
{
    private readonly DateOnly _checkIn;
    private readonly DateOnly _checkOut;

    public DateRangeMustBeValidRule(DateOnly checkIn, DateOnly checkOut)
    {
        _checkIn = checkIn;
        _checkOut = checkOut;
    }

    public string Message => "Check-in date must be before check-out date.";

    public bool IsBroken() => _checkOut <= _checkIn;
}

public sealed class GuestCountMustBeValidRule : IBusinessRule
{
    private readonly int _adults;
    private readonly int _children;

    public GuestCountMustBeValidRule(int adults, int children)
    {
        _adults = adults;
        _children = children;
    }

    public string Message => "At least one adult is required and children cannot be negative.";

    public bool IsBroken() => _adults <= 0 || _children < 0;
}

public sealed class RoomCountMustBeValidRule : IBusinessRule
{
    private readonly int _rooms;

    public RoomCountMustBeValidRule(int rooms) => _rooms = rooms;

    public string Message => "At least one room is required.";

    public bool IsBroken() => _rooms <= 0;
}

public sealed class OccupancyMustFitRoomRule : IBusinessRule
{
    private readonly int _adults;
    private readonly int _children;
    private readonly int _rooms;
    private readonly int _maxAdults;
    private readonly int _maxChildren;
    private readonly int _maxGuests;

    public OccupancyMustFitRoomRule(
        int adults,
        int children,
        int rooms,
        int maxAdults,
        int maxChildren,
        int maxGuests)
    {
        _adults = adults;
        _children = children;
        _rooms = rooms;
        _maxAdults = maxAdults;
        _maxChildren = maxChildren;
        _maxGuests = maxGuests;
    }

    public string Message => "Guest occupancy exceeds room limits.";

    public bool IsBroken()
    {
        if (_rooms <= 0)
        {
            return true;
        }

        var adultsPerRoom = (int)Math.Ceiling(_adults / (double)_rooms);
        var childrenPerRoom = (int)Math.Ceiling(_children / (double)_rooms);
        var guestsPerRoom = (int)Math.Ceiling((_adults + _children) / (double)_rooms);

        return adultsPerRoom > _maxAdults
               || childrenPerRoom > _maxChildren
               || guestsPerRoom > _maxGuests;
    }
}

public sealed class RatingMustBeValidRule : IBusinessRule
{
    private readonly int _rating;

    public RatingMustBeValidRule(int rating) => _rating = rating;

    public string Message => "Rating must be between 1 and 5.";

    public bool IsBroken() => _rating is < 1 or > 5;
}

public sealed class LoyaltyPointsMustBeNonNegativeRule : IBusinessRule
{
    private readonly int _points;

    public LoyaltyPointsMustBeNonNegativeRule(int points) => _points = points;

    public string Message => "Loyalty points cannot be negative.";

    public bool IsBroken() => _points < 0;
}
