namespace PaymentRoutingPoc.Persistence.Models.Read;

/// <summary>
/// Tracks the progress of projections through the event stream.
/// Enables exactly-once event processing and recovery from failures.
/// </summary>
public class ProjectionCheckpoint
{
    /// <summary>
    /// The unique identifier for this projection.
    /// (e.g., "PaymentProjection" or "MerchantStatisticsProjection")
    /// </summary>
    public string ProjectionId { get; set; } = null!;

    /// <summary>
    /// The global version of the last event processed by this projection.
    /// Used to determine which events are pending for this projection.
    /// </summary>
    public long LastProcessedGlobalVersion { get; set; }

    /// <summary>
    /// When the last checkpoint was recorded.
    /// Useful for monitoring and troubleshooting projection lag.
    /// </summary>
    public DateTime? LastCheckpointTime { get; set; }

    /// <summary>
    /// Additional projection state (JSON).
    /// Can store projection-specific metadata for complex scenarios.
    /// </summary>
    public string? ProjectionState { get; set; }

    /// <summary>
    /// When this checkpoint was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; set; }
}
