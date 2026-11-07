namespace HotelResortBooking.Api.Configuration;

public sealed class LoggingOptions
{
    public const string SectionName = "AppLogging";

    public bool IncludeScopes { get; set; }

    public string CorrelationIdHeaderName { get; set; } = "X-Correlation-ID";
}
