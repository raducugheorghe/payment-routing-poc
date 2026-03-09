using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentRoutingPoc.Domain.Aggregates;
using PaymentRoutingPoc.Domain.Events;
using PaymentRoutingPoc.Domain.Repositories;
using PaymentRoutingPoc.Persistence.DbContexts;
using PaymentRoutingPoc.Persistence.Models.Write;
using PaymentRoutingPoc.Persistence.Serialization;

namespace PaymentRoutingPoc.Persistence.Repositories.EventStore;

/// <summary>
/// Implementation of event sourcing repository using SQLite and Entity Framework.
/// Provides atomicity, idempotency, and optimistic concurrency control.
/// </summary>
public class EventRepository : IEventRepository
{
    private readonly WriteDbContext _writeDb;
    private readonly EventSerializer _eventSerializer;
    private readonly ILogger<EventRepository> _logger;

    public EventRepository(
        WriteDbContext writeDb,
        EventSerializer eventSerializer,
        ILogger<EventRepository> logger)
    {
        _writeDb = writeDb ?? throw new ArgumentNullException(nameof(writeDb));
        _eventSerializer = eventSerializer ?? throw new ArgumentNullException(nameof(eventSerializer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Appends events to the event store with idempotency and concurrency control.
    /// </summary>
    public async Task AppendEventsAsync(
        Guid aggregateId,
        string aggregateType,
        List<IDomainEvent> domainEvents,
        dynamic? metadata = null,
        int expectedVersion = 0,
        CancellationToken cancellationToken = default)
    {
        if (aggregateId == Guid.Empty)
            throw new ArgumentException("Aggregate ID cannot be empty", nameof(aggregateId));

        ArgumentException.ThrowIfNullOrWhiteSpace(aggregateType);

        if (domainEvents == null || domainEvents.Count == 0)
            throw new ArgumentException("Domain events list cannot be null or empty", nameof(domainEvents));

        var aggregateIdString = aggregateId.ToString();
        var eventMetadata = metadata as EventMetadata ?? new EventMetadata();

        using var transaction = await _writeDb.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, 
            cancellationToken);

        try
        {
            // Check for idempotency using idempotency key
            if (!string.IsNullOrWhiteSpace(eventMetadata.IdempotencyKey))
            {
                var existingEvent = await _writeDb.Events
                    .Where(e => e.IdempotencyKey == eventMetadata.IdempotencyKey)
                    .FirstOrDefaultAsync(cancellationToken);

                if (existingEvent != null)
                {
                    _logger.LogInformation(
                        "Duplicate event detected for idempotency key: {IdempotencyKey}. Skipping insertion.",
                        eventMetadata.IdempotencyKey);
                    
                    await transaction.RollbackAsync(cancellationToken);
                    return;
                }
            }

            // Check for concurrency conflicts (optimistic locking)
            var currentVersion = await _writeDb.Events
                .Where(e => e.EventStreamId == aggregateIdString)
                .Select(e => (int?)e.AggregateVersion)
                .MaxAsync(cancellationToken) ?? 0;

            if (currentVersion != expectedVersion)
            {
                throw new ConcurrencyException(
                    $"Expected aggregate version {expectedVersion}, but found {currentVersion}. " +
                    "The aggregate was modified concurrently. Reload and retry.");
            }

            // Get the next global version
            var lastGlobalVersion = await _writeDb.Events
                .OrderByDescending(e => e.GlobalVersion)
                .Select(e => e.GlobalVersion)
                .FirstOrDefaultAsync(cancellationToken);

            var nextGlobalVersion = lastGlobalVersion + 1;

            // Serialize metadata
            var metadataJson = _eventSerializer.SerializeMetadata(eventMetadata);

            // Append each event
            for (var i = 0; i < domainEvents.Count; i++)
            {
                var domainEvent = domainEvents[i];
                var storedEvent = new StoredEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    EventStreamId = aggregateIdString,
                    AggregateType = aggregateType,
                    AggregateVersion = domainEvent.AggregateVersion,
                    GlobalVersion = nextGlobalVersion++,
                    EventType = domainEvent.GetType().Name,
                    EventData = _eventSerializer.Serialize(domainEvent),
                    Metadata = metadataJson,
                    IdempotencyKey = i == 0 ? eventMetadata.IdempotencyKey : null,
                    OccurredAt = domainEvent.OccurredAt,
                    RecordedAt = DateTime.UtcNow,
                    IsCommitted = true
                };

                _writeDb.Events.Add(storedEvent);
            }

            await _writeDb.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully appended {EventCount} events for aggregate {AggregateId} (type: {AggregateType})",
                domainEvents.Count,
                aggregateId,
                aggregateType);
        }
        catch (DbUpdateException dbEx)
        {
            await transaction.RollbackAsync(cancellationToken);

            // DB-level uniqueness on idempotency key is the final guard for concurrent retries.
            if (!string.IsNullOrWhiteSpace(eventMetadata.IdempotencyKey) &&
                dbEx.InnerException?.Message.Contains("idx_idempotency_key", StringComparison.OrdinalIgnoreCase) == true)
            {
                _logger.LogInformation(
                    "Idempotent duplicate detected by unique constraint for key {IdempotencyKey}. Skipping insertion.",
                    eventMetadata.IdempotencyKey);
                return;
            }

            _logger.LogError(dbEx, "Database error while appending events for aggregate {AggregateId}", aggregateId);
            throw new PersistenceException(
                $"Failed to append events to event store. Aggregate: {aggregateId}",
                dbEx);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error appending events for aggregate {AggregateId}", aggregateId);
            throw;
        }
    }

    /// <summary>
    /// Retrieves all events for an aggregate from a specific version.
    /// </summary>
    public async Task<List<IDomainEvent>> GetEventsAsync(
        Guid aggregateId,
        int fromVersion = 0,
        CancellationToken cancellationToken = default)
    {
        if (aggregateId == Guid.Empty)
            throw new ArgumentException("Aggregate ID cannot be empty", nameof(aggregateId));

        var aggregateIdString = aggregateId.ToString();

        var storedEvents = await _writeDb.Events
            .Where(e => e.EventStreamId == aggregateIdString && e.AggregateVersion >= fromVersion)
            .OrderBy(e => e.AggregateVersion)
            .ToListAsync(cancellationToken);

        var domainEvents = new List<IDomainEvent>(storedEvents.Count);

        foreach (var storedEvent in storedEvents)
        {
            try
            {
                var domainEvent = _eventSerializer.Deserialize(storedEvent.EventData, storedEvent.EventType);
                domainEvents.Add(domainEvent);
            }
            catch (SerializationException ex)
            {
                _logger.LogError(ex,
                    "Failed to deserialize event {EventId} of type {EventType} for aggregate {AggregateId}",
                    storedEvent.EventId,
                    storedEvent.EventType,
                    aggregateId);

                throw new PersistenceException(
                    $"Failed to deserialize stored event. EventId: {storedEvent.EventId}, Type: {storedEvent.EventType}",
                    ex);
            }
        }

        _logger.LogInformation(
            "Retrieved {EventCount} events for aggregate {AggregateId} (from version {FromVersion})",
            domainEvents.Count,
            aggregateId,
            fromVersion);

        return domainEvents;
    }

    /// <summary>
    /// Loads an aggregate with snapshot optimization.
    /// </summary>
    public async Task<(TAggregate aggregate, int version)?> GetAggregateWithSnapshotAsync<TAggregate>(
        Guid aggregateId,
        Func<IEnumerable<IDomainEvent>, TAggregate> rehydrate,
        CancellationToken cancellationToken = default) where TAggregate : class
        , IEventSourcedAggregate
    {
        if (aggregateId == Guid.Empty)
            throw new ArgumentException("Aggregate ID cannot be empty", nameof(aggregateId));
        ArgumentNullException.ThrowIfNull(rehydrate);

        var aggregateIdString = aggregateId.ToString();
        var aggregateTypeName = typeof(TAggregate).Name;

        // Try to load snapshot
        var snapshot = await _writeDb.Snapshots
            .Where(s => s.EventStreamId == aggregateIdString && s.AggregateType == aggregateTypeName)
            .OrderByDescending(s => s.AggregateVersion)
            .FirstOrDefaultAsync(cancellationToken);

        TAggregate? aggregate = null;
        var startVersion = 0;
        var loadedVersion = 0;

        if (snapshot != null)
        {
            try
            {
                aggregate = JsonSerializer.Deserialize<TAggregate>(snapshot.AggregateData)
                           ?? throw new SerializationException("Deserialized snapshot is null");
                startVersion = snapshot.AggregateVersion + 1;
                loadedVersion = snapshot.AggregateVersion;

                _logger.LogInformation(
                    "Loaded aggregate {AggregateId} from snapshot at version {SnapshotVersion}",
                    aggregateId,
                    snapshot.AggregateVersion);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "Failed to deserialize snapshot for aggregate {AggregateId}. Will replay from events.",
                    aggregateId);
            }
        }

        // Load and replay events since snapshot (or from beginning if no snapshot)
        var events = await GetEventsAsync(aggregateId, startVersion, cancellationToken);

        if (events.Count == 0 && snapshot == null)
            return null; // No events and no snapshot = aggregate doesn't exist

        if (aggregate == null)
        {
            aggregate = rehydrate(events);
            loadedVersion = events[^1].AggregateVersion;
            return (aggregate, loadedVersion);
        }

        if (events.Count > 0)
        {
            foreach (var domainEvent in events)
            {
                aggregate.ApplyEvent(domainEvent);
            }
            loadedVersion = events[^1].AggregateVersion;
        }

        return (aggregate, loadedVersion);
    }

    /// <summary>
    /// Saves a snapshot of an aggregate for performance optimization.
    /// </summary>
    public async Task SaveSnapshotAsync(
        Guid aggregateId,
        object aggregate,
        int version,
        CancellationToken cancellationToken = default)
    {
        if (aggregateId == Guid.Empty)
            throw new ArgumentException("Aggregate ID cannot be empty", nameof(aggregateId));

        ArgumentNullException.ThrowIfNull(aggregate);

        if (version < 0)
            throw new ArgumentException("Version cannot be negative", nameof(version));

        var aggregateIdString = aggregateId.ToString();
        var aggregateTypeName = aggregate.GetType().Name;

        using var transaction = await _writeDb.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable,
            cancellationToken);

        try
        {
            // Check if there's already a newer snapshot
            var existingSnapshot = await _writeDb.Snapshots
                .Where(s => s.EventStreamId == aggregateIdString && s.AggregateType == aggregateTypeName)
                .OrderByDescending(s => s.AggregateVersion)
                .FirstOrDefaultAsync(cancellationToken);

            if (existingSnapshot != null && existingSnapshot.AggregateVersion >= version)
            {
                _logger.LogInformation(
                    "Snapshot already exists for aggregate {AggregateId} at version {ExistingVersion}. Skipping snapshot creation at version {RequestedVersion}.",
                    aggregateId,
                    existingSnapshot.AggregateVersion,
                    version);

                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            // Create or update snapshot
            var snapshot = new Snapshot
            {
                SnapshotId = Guid.NewGuid().ToString(),
                EventStreamId = aggregateIdString,
                AggregateType = aggregateTypeName,
                AggregateVersion = version,
                AggregateData = JsonSerializer.Serialize(aggregate),
                CreatedAt = DateTime.UtcNow
            };

            // Remove old snapshot if it exists
            if (existingSnapshot != null)
            {
                _writeDb.Snapshots.Remove(existingSnapshot);
            }

            _writeDb.Snapshots.Add(snapshot);
            await _writeDb.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "Snapshot saved for aggregate {AggregateId} at version {Version}",
                aggregateId,
                version);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error saving snapshot for aggregate {AggregateId}", aggregateId);
            throw new PersistenceException(
                $"Failed to save snapshot for aggregate {aggregateId}",
                ex);
        }
    }

    public async Task<Guid?> GetAggregateIdByIdempotencyKeyAsync(
        string idempotencyKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);

        var streamId = await _writeDb.Events
            .Where(e => e.IdempotencyKey == idempotencyKey)
            .OrderBy(e => e.RecordedAt)
            .Select(e => e.EventStreamId)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(streamId))
            return null;

        return Guid.TryParse(streamId, out var aggregateId)
            ? aggregateId
            : null;
    }
}

/// <summary>
/// Exception thrown when a concurrency conflict is detected.
/// Indicates that an aggregate was modified between read and write.
/// </summary>
public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message) : base(message) { }
}

/// <summary>
/// Exception thrown when persistence operations fail.
/// </summary>
public class PersistenceException : Exception
{
    public PersistenceException(string message) : base(message) { }
    public PersistenceException(string message, Exception innerException)
        : base(message, innerException) { }
}
