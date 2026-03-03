namespace PaymentRoutingPoc.Domain.Aggregates;

using Entities;
using Events;
using ValueObjects;

public class Payment
{
    private readonly List<object> _domainEvents = [];

    public Guid Id { get; private set; }
    public Money Total { get; private set; }
    public Card Card { get; private set; }
    public PaymentStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? ProcessedAt { get; private set; }
    public string? ProviderTransactionId { get; private set; }

    private Payment() { }

    public static Payment CreatePayment(Money total, Card card)
    {
        var payment = new Payment();
        
        if (total == null)
            throw new ArgumentNullException(nameof(total), "Total cannot be null");

        if (card == null)
            throw new ArgumentNullException(nameof(card), "Card cannot be null");

        payment.Id = Guid.NewGuid();
        payment.Total = total;
        payment.Card = card;
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
        if (Status == PaymentStatus.Processed)
            throw new InvalidOperationException("Payment is already processed");

        if (Status == PaymentStatus.Failed)
            throw new InvalidOperationException("Cannot process a failed payment");

        Status = PaymentStatus.Processed;
        ProcessedAt = DateTime.UtcNow;
        ProviderTransactionId = providerTransactionId;
        _domainEvents.Add(new PaymentSucceededEvent(Id, Total.Amount, Total.Currency));
    }

    public void MarkAsFailed(string reason)
    {
        if (Status == PaymentStatus.Failed)
            throw new InvalidOperationException("Payment is already marked as failed");

        if (Status == PaymentStatus.Processed)
            throw new InvalidOperationException("Cannot fail a processed payment");

        Status = PaymentStatus.Failed;
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