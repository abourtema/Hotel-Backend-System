namespace HotelResortBooking.Domain.Common;

/// <summary>
/// Converts <see cref="DateTimeOffset"/> values to UTC instants (offset zero).
/// Npgsql only accepts offset 0 for PostgreSQL <c>timestamptz</c>.
/// </summary>
public static class DateTimeOffsetExtensions
{
    public static DateTimeOffset ToUtc(this DateTimeOffset value)
        => value.Offset == TimeSpan.Zero ? value : value.ToOffset(TimeSpan.Zero);

    public static DateTimeOffset? ToUtc(this DateTimeOffset? value)
        => value?.ToUtc();
}
