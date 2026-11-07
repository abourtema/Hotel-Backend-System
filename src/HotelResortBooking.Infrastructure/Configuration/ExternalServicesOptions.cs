namespace HotelResortBooking.Infrastructure.Configuration;

public sealed class ExternalServicesOptions
{
    public const string SectionName = "ExternalServices";

    public string PaymentProviderBaseUrl { get; set; } = string.Empty;

    public string EmailProviderBaseUrl { get; set; } = string.Empty;

    public string StorageProviderBaseUrl { get; set; } = string.Empty;
}
