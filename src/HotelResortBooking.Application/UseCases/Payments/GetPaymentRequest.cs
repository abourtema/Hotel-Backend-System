namespace HotelResortBooking.Application.UseCases.Payments;

public sealed class GetPaymentRequest
{
    public Guid PaymentId { get; init; }

    public bool BypassOwnershipCheck { get; init; }
}
