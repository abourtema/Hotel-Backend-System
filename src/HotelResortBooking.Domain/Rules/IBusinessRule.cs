namespace HotelResortBooking.Domain.Rules;

/// <summary>
/// Contract for domain business rules evaluated within the domain layer.
/// </summary>
public interface IBusinessRule
{
    string Message { get; }

    bool IsBroken();
}
