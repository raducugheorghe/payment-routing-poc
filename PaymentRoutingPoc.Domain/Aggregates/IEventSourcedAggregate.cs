namespace PaymentRoutingPoc.Domain.Aggregates;

using Events;

/// <summary>
/// Contract for aggregates that can be reconstructed from event streams.
/// </summary>
public interface IEventSourcedAggregate
{
    void ApplyEvent(IDomainEvent domainEvent);
}
