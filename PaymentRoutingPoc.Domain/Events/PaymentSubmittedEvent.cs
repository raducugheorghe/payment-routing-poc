namespace PaymentRoutingPoc.Domain.Events;

public class PaymentSubmittedEvent
{
    public Guid PaymentId { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public DateTime OccurredAt { get; }

    public PaymentSubmittedEvent(Guid paymentId, decimal amount, string currency)
    {
        PaymentId = paymentId;
        Amount = amount;
        Currency = currency;
        OccurredAt = DateTime.UtcNow;
    }
}

