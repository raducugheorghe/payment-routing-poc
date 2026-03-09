namespace PaymentRoutingPoc.Persistence.Serialization;

/// <summary>
/// Metadata associated with domain events for tracing, idempotency, and audit purposes.
/// Stored separately from event data to support cross-cutting concerns.
/// </summary>
public class EventMetadata
{
    /// <summary>
    /// Unique key for idempotent event processing.
    /// If the same event is processed twice with the same key, the second is rejected.
    /// Prevents duplicate event storage on client-side retries.
    /// </summary>
    public string? IdempotencyKey { get; set; }

    /// <summary>
    /// Request ID for tracing this specific request.
    /// Typically matches the HTTP request ID or command ID.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Correlation ID for tracing across the entire distributed transaction.
    /// Links multiple events, commands, and queries in a single business operation.
    /// </summary>
    public string? CorrelationId { get; set; }

    /// <summary>
    /// The command or event that caused this event to be created.
    /// Shows the causation chain: Command → Event → Projection
    /// </summary>
    public string? CausationId { get; set; }

    /// <summary>
    /// The user who initiated the operation that produced this event.
    /// Useful for audit trails and access control auditing.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Timestamp when the event was processed (separate from OccurredAt).
    /// OccurredAt is the business time, this is the system time.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
