namespace PaymentRoutingPoc.Domain.Events;

public class PaymentSucceededEvent
{
    public Guid PaymentId { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public DateTime OccurredAt { get; }

    public PaymentSucceededEvent(Guid paymentId, decimal amount, string currency)
    {
        PaymentId = paymentId;
        Amount = amount;
        Currency = currency;
        OccurredAt = DateTime.UtcNow;
    }
}

