using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentRoutingPoc.Persistence.DbContexts;
using PaymentRoutingPoc.Persistence.Models.Read;

namespace PaymentRoutingPoc.Persistence.Projections;

/// <summary>
/// Orchestrates CQRS projections with checkpoint-based idempotency.
/// Ensures each event is processed exactly once per projection despite retries or failures.
/// </summary>
public class ProjectionProcessor
{
    private readonly WriteDbContext _writeDb;
    private readonly ReadDbContext _readDb;
    private readonly Serialization.EventSerializer _eventSerializer;
    private readonly ILogger<ProjectionProcessor> _logger;

    public ProjectionProcessor(
        WriteDbContext writeDb,
        ReadDbContext readDb,
        Serialization.EventSerializer eventSerializer,
        ILogger<ProjectionProcessor> logger)
    {
        _writeDb = writeDb ?? throw new ArgumentNullException(nameof(writeDb));
        _readDb = readDb ?? throw new ArgumentNullException(nameof(readDb));
        _eventSerializer = eventSerializer ?? throw new ArgumentNullException(nameof(eventSerializer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Processes pending events for all projections.
    /// Tracks progress with checkpoints to enable recovery from failures.
    /// </summary>
    /// <param name="projections">List of projections to update</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Number of events processed</returns>
    public async Task<int> ProcessPendingEventsAsync(
        IEnumerable<IProjection> projections,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(projections);

        var projectionList = projections.ToList();
        if (projectionList.Count == 0)
        {
            _logger.LogWarning("No projections provided to ProjectionProcessor");
            return 0;
        }

        var totalEventsProcessed = 0;
        var failures = new List<Exception>();

        foreach (var projection in projectionList)
        {
            try
            {
                var eventsProcessed = await ProcessProjectionAsync(projection, cancellationToken);
                totalEventsProcessed += eventsProcessed;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing projection {ProjectionId}",
                    projection.ProjectionId);
                failures.Add(new InvalidOperationException(
                    $"Projection {projection.ProjectionId} failed", ex));
            }
        }

        if (failures.Count > 0)
            throw new AggregateException("One or more projections failed during processing", failures);

        return totalEventsProcessed;
    }

    /// <summary>
    /// Processes pending events for a specific projection.
    /// </summary>
    private async Task<int> ProcessProjectionAsync(
        IProjection projection,
        CancellationToken cancellationToken)
    {
        var projectionId = projection.ProjectionId;

        _readDb.ChangeTracker.Clear();

        var existingCheckpoint = await _readDb.ProjectionCheckpoints
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.ProjectionId == projectionId, cancellationToken);

        var checkpoint = existingCheckpoint ?? new ProjectionCheckpoint
        {
            ProjectionId = projectionId,
            LastProcessedGlobalVersion = 0,
            UpdatedAt = DateTime.UtcNow
        };

        var startVersion = checkpoint.LastProcessedGlobalVersion;

        // Get pending events
        var pendingEvents = await _writeDb.Events
            .Where(e => e.GlobalVersion > startVersion)
            .OrderBy(e => e.GlobalVersion)
            .ToListAsync(cancellationToken);

        if (pendingEvents.Count == 0)
        {
            _logger.LogDebug(
                "No pending events for projection {ProjectionId}. Last processed version: {LastVersion}",
                projectionId,
                startVersion);
            return 0;
        }

        _logger.LogInformation(
            "Processing {EventCount} pending events for projection {ProjectionId}. Starting from version {StartVersion}",
            pendingEvents.Count,
            projectionId,
            startVersion);

        // Deserialize and process events
        var successCount = 0;
        long lastProcessedVersion = startVersion;

        foreach (var storedEvent in pendingEvents)
        {
            try
            {
                // Deserialize event
                var domainEvent = _eventSerializer.Deserialize(storedEvent.EventData, storedEvent.EventType);

                await using var tx = await _readDb.Database.BeginTransactionAsync(cancellationToken);

                // Process through projection (should be idempotent)
                await projection.HandleAsync(domainEvent, cancellationToken);

                // Advance checkpoint in the same transaction as read-model writes.
                checkpoint.LastProcessedGlobalVersion = storedEvent.GlobalVersion;
                checkpoint.LastCheckpointTime = DateTime.UtcNow;
                checkpoint.UpdatedAt = DateTime.UtcNow;

                if (existingCheckpoint == null)
                {
                    _readDb.ProjectionCheckpoints.Add(checkpoint);
                    existingCheckpoint = checkpoint;
                }
                else
                {
                    _readDb.ProjectionCheckpoints.Update(checkpoint);
                }

                await _readDb.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);

                successCount++;
                lastProcessedVersion = storedEvent.GlobalVersion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error processing event {EventId} (type: {EventType}) for projection {ProjectionId}",
                    storedEvent.EventId,
                    storedEvent.EventType,
                    projectionId);

                throw; // Let caller decide if we should continue or fail
            }
        }

        _logger.LogInformation(
            "Completed processing for projection {ProjectionId}. Processed {EventCount} events. New checkpoint: {CheckpointVersion}",
            projectionId,
            successCount,
            lastProcessedVersion);

        return successCount;
    }

    /// <summary>
    /// Gets the current checkpoint progress for a projection.
    /// Useful for monitoring and diagnostics.
    /// </summary>
    public async Task<ProjectionCheckpoint?> GetCheckpointAsync(
        string projectionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionId);

        return await _readDb.ProjectionCheckpoints
            .FirstOrDefaultAsync(c => c.ProjectionId == projectionId, cancellationToken);
    }

    /// <summary>
    /// Resets a projection's checkpoint, causing it to reprocess all events.
    /// Useful for fixing projection inconsistencies or during debugging.
    /// </summary>
    public async Task ResetProjectionAsync(
        string projectionId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectionId);

        var checkpoint = await _readDb.ProjectionCheckpoints
            .FirstOrDefaultAsync(c => c.ProjectionId == projectionId, cancellationToken);

        if (checkpoint != null)
        {
            checkpoint.LastProcessedGlobalVersion = 0;
            checkpoint.LastCheckpointTime = null;
            checkpoint.UpdatedAt = DateTime.UtcNow;

            _readDb.ProjectionCheckpoints.Update(checkpoint);
            await _readDb.SaveChangesAsync(cancellationToken);

            _logger.LogWarning(
                "Reset checkpoint for projection {ProjectionId}. Will reprocess all events.",
                projectionId);
        }
    }
}
