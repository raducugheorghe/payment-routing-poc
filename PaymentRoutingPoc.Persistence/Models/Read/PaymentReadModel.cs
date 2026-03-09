namespace PaymentRoutingPoc.Persistence.Models.Read;

/// <summary>
/// Denormalized read model for Payment queries.
/// Optimized for fast retrieval and analysis.
/// Updated through projections when domain events occur.
/// </summary>
public class PaymentReadModel
{
    /// <summary>
    /// The unique payment identifier.
    /// </summary>
    public string PaymentId { get; set; } = null!;

    /// <summary>
    /// The payment amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// The currency code (e.g., "USD", "EUR").
    /// </summary>
    public string Currency { get; set; } = null!;

    /// <summary>
    /// Current status: Pending, Processing, Processed, Failed.
    /// </summary>
    public string Status { get; set; } = null!;

    /// <summary>
    /// The card ID used for this payment.
    /// Denormalized from the Payment aggregate.
    /// </summary>
    public string CardId { get; set; } = null!;

    /// <summary>
    /// Masked or partial card number for display.
    /// </summary>
    public string CardNumber { get; set; } = null!;

    /// <summary>
    /// The merchant ID receiving the payment.
    /// </summary>
    public string MerchantId { get; set; } = null!;

    /// <summary>
    /// The merchant name for display.
    /// Denormalized for faster queries.
    /// </summary>
    public string MerchantName { get; set; } = null!;

    /// <summary>
    /// The payment provider's transaction ID.
    /// */
    public string? ProviderTransactionId { get; set; }

    /// <summary>
    /// The payment provider name (e.g., "Stripe", "PaymentGateway").
    /// </summary>
    public string? ProviderName { get; set; }

    /// <summary>
    /// When this payment was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When this payment was successfully processed.
    /// Null until status becomes Processed.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Reason for failure if status is Failed.
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// The type of the last event that affected this payment.
    /// Useful for debugging and understanding state transitions.
    /// </summary>
    public string? LastEventType { get; set; }

    /// <summary>
    /// The aggregate version (number of events) in the event stream.
    /// Useful for understanding the complexity of this aggregate.
    /// </summary>
    public int AggregateVersion { get; set; }
}
