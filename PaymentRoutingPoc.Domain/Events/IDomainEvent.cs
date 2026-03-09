namespace PaymentRoutingPoc.Domain.Events;

/// <summary>
/// Base interface for all domain events in event sourcing pattern.
/// Provides essential metadata for event tracking, tracing, and rehydration.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// The aggregate ID that this event belongs to (e.g., Payment ID).
    /// </summary>
    Guid AggregateId { get; }

    /// <summary>
    /// When the event occurred in the business domain.
    /// </summary>
    DateTime OccurredAt { get; }

    /// <summary>
    /// The version of the event within the aggregate stream.
    /// Used to reconstruct aggregates in correct order.
    /// </summary>
    int AggregateVersion { get; }
}
