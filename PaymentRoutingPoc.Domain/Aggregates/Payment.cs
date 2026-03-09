namespace PaymentRoutingPoc.Domain.Aggregates;

using Entities;
using Events;
using ValueObjects;

public class Payment: EntityBase, IEventSourcedAggregate
{
    private readonly List<IDomainEvent> _domainEvents = [];
    
    public Money Total { get; private set; } = null!;
    public Card Card { get; private set; } = null!;
    public Merchant Merchant { get; private set; } = null!;
    public PaymentStatus Status { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? ProviderTransactionId { get; private set; }
    public int Version { get; private set; }

    private Payment() { }

    public static Payment CreatePayment(Money total, Card card, Merchant merchant)
    {
        var payment = new Payment();
        
        if (total == null)
            throw new ArgumentNullException(nameof(total), "Total cannot be null");

        if (card == null)
            throw new ArgumentNullException(nameof(card), "Card cannot be null");
        
        if (merchant == null)
            throw new ArgumentNullException(nameof(merchant), "Merchant cannot be null");

        payment.Id = Guid.NewGuid();
        payment.Total = total;
        payment.Card = card;
        payment.Merchant = merchant;
        payment.Status = PaymentStatus.Pending;
        payment.CreatedAt = DateTime.UtcNow;
        payment.Version = 0;

        return payment;
    }

    public static Payment RehydrateFromEvents(IEnumerable<IDomainEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        var orderedEvents = events
            .OrderBy(e => e.AggregateVersion)
            .ToList();

        if (orderedEvents.Count == 0)
            throw new ArgumentException("Cannot rehydrate Payment from an empty event stream", nameof(events));

        if (orderedEvents[0] is not PaymentSubmittedEvent)
            throw new InvalidOperationException("Invalid payment event stream: first event must be PaymentSubmittedEvent");

        var expectedVersion = 1;
        foreach (var domainEvent in orderedEvents)
        {
            if (domainEvent.AggregateVersion != expectedVersion)
                throw new InvalidOperationException(
                    $"Invalid payment event stream: expected version {expectedVersion} but found {domainEvent.AggregateVersion}");

            expectedVersion++;
        }

        var payment = new Payment();
        foreach (var domainEvent in orderedEvents)
        {
            payment.ApplyEvent(domainEvent);
        }

        return payment;
    }

    public void Submit()
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot submit a payment with status {Status}");

        var domainEvent = new PaymentSubmittedEvent(
            Id,
            Total.Amount,
            Total.Currency,
            Card.Id,
            Card.GetLast4(),
            Merchant.Id,
            Merchant.Name,
            Version + 1);

        ApplyEvent(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public void MarkAsProcessed(string providerTransactionId, string providerName)
    {
        if (!Status.CanTransitionTo(PaymentStatus.Processed))
            throw new InvalidOperationException($"Cannot process payment with status {Status}");

        ArgumentException.ThrowIfNullOrWhiteSpace(providerTransactionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerName);

        var domainEvent = new PaymentSucceededEvent(
            Id,
            Total.Amount,
            Total.Currency,
            providerTransactionId,
            providerName,
            Version + 1);

        ApplyEvent(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public void MarkAsFailed(string reason)
    {
        if (!Status.CanTransitionTo(PaymentStatus.Failed))
            throw new InvalidOperationException($"Cannot fail payment with status {Status}");

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        var domainEvent = new PaymentFailedEvent(Id, Total.Amount, Total.Currency, reason, Version + 1);

        ApplyEvent(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    public void ApplyEvent(IDomainEvent domainEvent)
    {
        switch (domainEvent)
        {
            case PaymentSubmittedEvent submittedEvent:
                ApplyPaymentSubmitted(submittedEvent);
                break;
            case PaymentSucceededEvent succeededEvent:
                ApplyPaymentSucceeded(succeededEvent);
                break;
            case PaymentFailedEvent failedEvent:
                ApplyPaymentFailed(failedEvent);
                break;
            default:
                throw new InvalidOperationException($"Unsupported event type: {domainEvent.GetType().Name}");
        }

        Version = domainEvent.AggregateVersion;
    }

    private void ApplyPaymentSubmitted(PaymentSubmittedEvent submittedEvent)
    {
        Id = submittedEvent.PaymentId;
        Total = Money.From((submittedEvent.Amount, submittedEvent.Currency));
        Status = PaymentStatus.Pending;
        CreatedAt = submittedEvent.OccurredAt;
        ProcessedAt = null;

        Card = Card.LoadCard(submittedEvent.CardId, Card.BuildMaskedFromLast4(submittedEvent.CardLast4));
        Merchant = Merchant.LoadMerchant(submittedEvent.MerchantId, submittedEvent.MerchantName);
    }

    private void ApplyPaymentSucceeded(PaymentSucceededEvent succeededEvent)
    {
        if (Status != null)
        {
            Status = Status.TransitionTo(PaymentStatus.Processed);
        }
        else
        {
            Status = PaymentStatus.Processed;
        }

        ProcessedAt = succeededEvent.OccurredAt;
        ProviderTransactionId = succeededEvent.ProviderTransactionId;
    }

    private void ApplyPaymentFailed(PaymentFailedEvent failedEvent)
    {
        if (Status != null)
        {
            Status = Status.TransitionTo(PaymentStatus.Failed);
        }
        else
        {
            Status = PaymentStatus.Failed;
        }

        ProcessedAt = failedEvent.OccurredAt;
    }

    public IReadOnlyList<IDomainEvent> GetDomainEvents()
    {
        return _domainEvents.AsReadOnly();
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}