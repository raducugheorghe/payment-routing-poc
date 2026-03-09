namespace PaymentRoutingPoc.Persistence.Models.Read;

/// <summary>
/// Audit trail of events affecting each payment.
/// Provides a complete history for compliance and debugging.
/// </summary>
public class PaymentEventLog
{
    /// <summary>
    /// Unique identifier for this audit log entry.
    /// </summary>
    public string EventLogId { get; set; } = null!;

    /// <summary>
    /// The payment ID this event is associated with.
    /// </summary>
    public string PaymentId { get; set; } = null!;

    /// <summary>
    /// The type of event (e.g., "PaymentSubmittedEvent").
    /// </summary>
    public string EventType { get; set; } = null!;

    /// <summary>
    /// The complete event data in JSON format.
    /// Provides full transparency into what changed and why.
    /// </summary>
    public string EventData { get; set; } = null!;

    /// <summary>
    /// When the event occurred.
    /// </summary>
    public DateTime OccurredAt { get; set; }
}
