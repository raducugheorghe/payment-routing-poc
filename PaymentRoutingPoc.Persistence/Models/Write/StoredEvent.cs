namespace PaymentRoutingPoc.Persistence.Models.Write;

/// <summary>
/// Represents a stored domain event in the event store.
/// Events are immutable and form the complete audit trail of an aggregate.
/// </summary>
public class StoredEvent
{
    /// <summary>
    /// Unique identifier for this specific event instance.
    /// </summary>
    public string EventId { get; set; } = null!;

    /// <summary>
    /// The aggregate ID (e.g., Payment ID) that this event belongs to.
    /// Groups events by their aggregate stream.
    /// </summary>
    public string EventStreamId { get; set; } = null!;

    /// <summary>
    /// The type of aggregate being modified (e.g., "Payment").
    /// </summary>
    public string AggregateType { get; set; } = null!;

    /// <summary>
    /// The version of this event within the aggregate stream.
    /// Starts at 1 and increments for each event in the stream.
    /// Together with EventStreamId, this forms a unique key.
    /// </summary>
    public int AggregateVersion { get; set; }

    /// <summary>
    /// A globally unique sequence number across all events.
    /// Used for ordering projections and ensuring idempotency.
    /// </summary>
    public long GlobalVersion { get; set; }

    /// <summary>
    /// The type name of the domain event (e.g., "PaymentSubmittedEvent").
    /// Used during deserialization to instantiate the correct event type.
    /// </summary>
    public string EventType { get; set; } = null!;

    /// <summary>
    /// The serialized event data in JSON format.
    /// Stores all event-specific state needed for event sourcing.
    /// </summary>
    public string EventData { get; set; } = null!;

    /// <summary>
    /// Metadata about the event (JSON):
    /// - IdempotencyKey: For duplicate detection
    /// - RequestId: Request tracing
    /// - CorrelationId: Distributed tracing
    /// - CausationId: Command that caused this event
    /// - UserId: User who triggered this event
    /// - Timestamp: When the event was processed
    /// </summary>
    public string Metadata { get; set; } = null!;

    /// <summary>
    /// Idempotency key extracted from metadata for exact-match deduplication.
    /// </summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// When the event occurred in the business domain.
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// When the event was recorded into the event store.
    /// </summary>
    public DateTime RecordedAt { get; set; }

    /// <summary>
    /// Whether the event has been committed.
    /// Allows for delayed commit patterns if needed in the future.
    /// </summary>
    public bool IsCommitted { get; set; }
}
