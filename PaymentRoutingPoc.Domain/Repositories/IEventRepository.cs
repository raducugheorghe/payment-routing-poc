namespace PaymentRoutingPoc.Domain.Repositories;

using Aggregates;
using Events;

/// <summary>
/// Interface for event sourcing repository.
/// Responsible for storing and retrieving immutable domain events.
/// </summary>
public interface IEventRepository
{
    /// <summary>
    /// Appends new domain events to the event store for an aggregate.
    /// Ensures idempotent behavior using idempotency keys.
    /// Detects and prevents concurrent modifications with optimistic locking.
    /// </summary>
    /// <param name="aggregateId">The ID of the aggregate being modified</param>
    /// <param name="aggregateType">The type name of the aggregate (e.g., "Payment")</param>
    /// <param name="domainEvents">List of domain events to append</param>
    /// <param name="metadata">Event metadata for tracing and idempotency</param>
    /// <param name="expectedVersion">The aggregate version we expect before appending.
    /// If actual version doesn't match, throws ConcurrencyException</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task completed when events are persisted</returns>
    Task AppendEventsAsync(
        Guid aggregateId,
        string aggregateType,
        List<IDomainEvent> domainEvents,
        dynamic? metadata = null,
        int expectedVersion = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all events for an aggregate from the event store, optionally
    /// from a specific version onwards.
    /// </summary>
    /// <param name="aggregateId">The ID of the aggregate</param>
    /// <param name="fromVersion">Start from this version (0 = from beginning)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of domain events in order</returns>
    Task<List<IDomainEvent>> GetEventsAsync(
        Guid aggregateId,
        int fromVersion = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads an aggregate with the most recent snapshot, then replays events
    /// after the snapshot version.
    /// Useful for aggregates with many events (100+).
    /// </summary>
    /// <typeparam name="TAggregate">The aggregate type</typeparam>
    /// <param name="aggregateId">The ID of the aggregate</param>
    /// <param name="rehydrate">Factory that creates aggregate from ordered events when no snapshot exists</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Tuple of (aggregate, loadedVersion) if found, null otherwise</returns>
    Task<(TAggregate aggregate, int version)?> GetAggregateWithSnapshotAsync<TAggregate>(
        Guid aggregateId,
        Func<IEnumerable<IDomainEvent>, TAggregate> rehydrate,
        CancellationToken cancellationToken = default) where TAggregate : class, IEventSourcedAggregate;

    /// <summary>
    /// Saves a snapshot of an aggregate at a specific version.
    /// Snapshots are used for performance optimization of frequently-accessed aggregates.
    /// </summary>
    /// <param name="aggregateId">The ID of the aggregate</param>
    /// <param name="aggregate">The aggregate state to snapshot</param>
    /// <param name="version">The version at which this snapshot was taken</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task completed when snapshot is persisted</returns>
    Task SaveSnapshotAsync(
        Guid aggregateId,
        object aggregate,
        int version,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds the aggregate stream associated with an idempotency key.
    /// Returns null when no matching request has been recorded.
    /// </summary>
    Task<Guid?> GetAggregateIdByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default);
}
