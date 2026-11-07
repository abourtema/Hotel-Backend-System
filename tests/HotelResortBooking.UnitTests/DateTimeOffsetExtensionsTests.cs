using HotelResortBooking.Domain.Common;
using Xunit;

namespace HotelResortBooking.UnitTests;

public sealed class DateTimeOffsetExtensionsTests
{
    [Fact]
    public void ToUtc_ConvertsPlusTwoOffsetToEquivalentUtcInstant()
    {
        var local = new DateTimeOffset(2026, 9, 7, 0, 0, 0, TimeSpan.FromHours(2));

        var utc = local.ToUtc();

        Assert.Equal(TimeSpan.Zero, utc.Offset);
        Assert.Equal(new DateTimeOffset(2026, 9, 6, 22, 0, 0, TimeSpan.Zero), utc);
        Assert.Equal(local.UtcTicks, utc.UtcTicks);
    }

    [Fact]
    public void ToUtc_LeavesUtcValueUnchanged()
    {
        var utc = new DateTimeOffset(2026, 9, 7, 12, 0, 0, TimeSpan.Zero);

        Assert.Equal(utc, utc.ToUtc());
    }

    [Fact]
    public void ToUtc_Nullable_ConvertsOrReturnsNull()
    {
        DateTimeOffset? missing = null;
        DateTimeOffset? local = new DateTimeOffset(2026, 9, 7, 0, 0, 0, TimeSpan.FromHours(2));

        Assert.Null(missing.ToUtc());
        Assert.Equal(new DateTimeOffset(2026, 9, 6, 22, 0, 0, TimeSpan.Zero), local.ToUtc());
    }
}
