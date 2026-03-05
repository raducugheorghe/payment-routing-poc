namespace PaymentRoutingPoc.Domain.Aggregates;

using Entities;
using Events;
using ValueObjects;

public class Payment: EntityBase
{
    private readonly List<object> _domainEvents = [];
    
    public Money Total { get; private set; }
    public Card Card { get; private set; }
    public Merchant Merchant { get; private set; }
    public PaymentStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? ProviderTransactionId { get; private set; }

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

        return payment;
    }

    public void Submit()
    {
        if (Status != PaymentStatus.Pending)
            throw new InvalidOperationException($"Cannot submit a payment with status {Status}");

        _domainEvents.Add(new PaymentSubmittedEvent(Id, Total.Amount, Total.Currency));
    }

    public void MarkAsProcessed(string providerTransactionId)
    {
        Status = Status.TransitionTo(PaymentStatus.Processed);
        ProcessedAt = DateTime.UtcNow;
        ProviderTransactionId = providerTransactionId;
        _domainEvents.Add(new PaymentSucceededEvent(Id, Total.Amount, Total.Currency));
    }

    public void MarkAsFailed(string reason)
    {
        Status = Status.TransitionTo(PaymentStatus.Failed);
        ProcessedAt = DateTime.UtcNow;
        _domainEvents.Add(new PaymentFailedEvent(Id, Total.Amount, Total.Currency, reason));
    }

    public IReadOnlyList<object> GetDomainEvents()
    {
        return _domainEvents.AsReadOnly();
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}