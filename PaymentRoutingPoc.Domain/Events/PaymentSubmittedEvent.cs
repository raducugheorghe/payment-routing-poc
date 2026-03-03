namespace PaymentRoutingPoc.Domain.Events;

using MediatR;

public class PaymentSubmittedEvent : INotification
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

