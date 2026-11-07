namespace HotelResortBooking.Application.UseCases.Payments;

public sealed class HandlePaymentFailureRequest
{
    public Guid PaymentId { get; init; }

    public string? FailureReason { get; init; }

    public bool BypassOwnershipCheck { get; init; }
}
