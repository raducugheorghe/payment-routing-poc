namespace PaymentRoutingPoc.Persistence.Models.Write;

/// <summary>
/// Represents a snapshot of an aggregate's state.
/// Snapshots are optional optimization to avoid replaying 100+ events on every load.
/// </summary>
public class Snapshot
{
    /// <summary>
    /// Unique identifier for this snapshot.
    /// </summary>
    public string SnapshotId { get; set; } = null!;

    /// <summary>
    /// The aggregate ID (e.g., Payment ID) that this snapshot represents.
    /// </summary>
    public string EventStreamId { get; set; } = null!;

    /// <summary>
    /// The type of aggregate (e.g., "Payment").
    /// </summary>
    public string AggregateType { get; set; } = null!;

    /// <summary>
    /// The aggregate version at which this snapshot was taken.
    /// Allows loading the snapshot and replaying only newer events.
    /// </summary>
    public int AggregateVersion { get; set; }

    /// <summary>
    /// The serialized aggregate state in JSON format.
    /// Contains the complete aggregate state at AggregateVersion.
    /// </summary>
    public string AggregateData { get; set; } = null!;

    /// <summary>
    /// When this snapshot was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }
}
