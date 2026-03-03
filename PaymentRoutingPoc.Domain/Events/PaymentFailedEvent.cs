namespace PaymentRoutingPoc.Domain.Events;

using MediatR;

public class PaymentFailedEvent : INotification
{
    public Guid PaymentId { get; }
    public decimal Amount { get; }
    public string Currency { get; }
    public string Reason { get; }
    public DateTime OccurredAt { get; }

    public PaymentFailedEvent(Guid paymentId, decimal amount, string currency, string reason)
    {
        PaymentId = paymentId;
        Amount = amount;
        Currency = currency;
        Reason = reason;
        OccurredAt = DateTime.UtcNow;
    }
}

