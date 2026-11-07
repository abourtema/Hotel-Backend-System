using HotelResortBooking.Application.Abstractions;

namespace HotelResortBooking.UnitTests.Fakes;

public sealed class FakeAccessContext : IAccessContext
{
    public AccessMode Mode { get; set; } = AccessMode.Guest;

    public Guid? UserId { get; set; }

    public bool IsAuthenticated => Mode == AccessMode.Authenticated && UserId is not null;

    public void Authenticate(Guid userId)
    {
        Mode = AccessMode.Authenticated;
        UserId = userId;
    }

    public void AsGuest()
    {
        Mode = AccessMode.Guest;
        UserId = null;
    }
}
