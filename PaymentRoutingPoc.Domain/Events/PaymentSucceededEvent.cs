namespace PaymentRoutingPoc.Domain.Events;

public class PaymentSucceededEvent : IDomainEvent
{
    public Guid PaymentId { get; }
    public Guid AggregateId => PaymentId;
    
    public decimal Amount { get; }
    public string Currency { get; }
    public string ProviderTransactionId { get; }
    public string ProviderName { get; }
    public DateTime OccurredAt { get; }
    public int AggregateVersion { get; }

    public PaymentSucceededEvent(
        Guid paymentId,
        decimal amount,
        string currency,
        string providerTransactionId,
        string providerName,
        int aggregateVersion,
        DateTime occurredAt = default)
    {
        PaymentId = paymentId;
        Amount = amount;
        Currency = currency;
        ProviderTransactionId = providerTransactionId;
        ProviderName = providerName;
        OccurredAt = occurredAt == default ? DateTime.UtcNow : occurredAt;
        AggregateVersion = aggregateVersion;
    }
}

