namespace PaymentRoutingPoc.Domain.Events;

public class PaymentSubmittedEvent : IDomainEvent
{
    public Guid PaymentId { get; }
    public Guid AggregateId => PaymentId;
    
    public decimal Amount { get; }
    public string Currency { get; }
    public Guid CardId { get; }
    public string CardLast4 { get; }
    public Guid MerchantId { get; }
    public string MerchantName { get; }
    public DateTime OccurredAt { get; }
    public int AggregateVersion { get; }

    public PaymentSubmittedEvent(
        Guid paymentId,
        decimal amount,
        string currency,
        Guid cardId,
        string cardLast4,
        Guid merchantId,
        string merchantName,
        int aggregateVersion,
        DateTime occurredAt = default)
    {
        PaymentId = paymentId;
        Amount = amount;
        Currency = currency;
        CardId = cardId;
        CardLast4 = cardLast4;
        MerchantId = merchantId;
        MerchantName = merchantName;
        OccurredAt = occurredAt == default ? DateTime.UtcNow : occurredAt;
        AggregateVersion = aggregateVersion;
    }
}

