using PaymentRoutingPoc.Domain.Events;

namespace PaymentRoutingPoc.Persistence.Projections;

/// <summary>
/// Marker interface for CQRS projections.
/// Projections consume domain events and update the read model.
/// </summary>
public interface IProjection
{
    /// <summary>
    /// Unique identifier for this projection.
    /// Used for tracking checkpoint progress through event stream.
    /// </summary>
    string ProjectionId { get; }

    /// <summary>
    /// Handles a domain event and updates the read model.
    /// Should be idempotent - handling the same event twice should be safe.
    /// </summary>
    /// <param name="domainEvent">The event to process</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken);
}
