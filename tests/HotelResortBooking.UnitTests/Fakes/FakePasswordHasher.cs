using HotelResortBooking.Application.Abstractions.Repositories;

namespace HotelResortBooking.UnitTests.Fakes;

public sealed class FakePasswordHasher : IPasswordHasher
{
    public string Hash(string password) => "hash:" + password;

    public bool Verify(string password, string passwordHash) => Hash(password) == passwordHash;
}
