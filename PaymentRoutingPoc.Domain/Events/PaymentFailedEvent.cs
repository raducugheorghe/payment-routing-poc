namespace PaymentRoutingPoc.Domain.Events;

public class PaymentFailedEvent : IDomainEvent
{
    public Guid PaymentId { get; }
    public Guid AggregateId => PaymentId;
    
    public decimal Amount { get; }
    public string Currency { get; }
    public string Reason { get; }
    public DateTime OccurredAt { get; }
    public int AggregateVersion { get; }

    public PaymentFailedEvent(
        Guid paymentId,
        decimal amount,
        string currency,
        string reason,
         int aggregateVersion,
         DateTime occurredAt = default)
    {
        PaymentId = paymentId;
        Amount = amount;
        Currency = currency;
        Reason = reason;
        OccurredAt = occurredAt == default ? DateTime.UtcNow : occurredAt;
        AggregateVersion = aggregateVersion;
    }
}

