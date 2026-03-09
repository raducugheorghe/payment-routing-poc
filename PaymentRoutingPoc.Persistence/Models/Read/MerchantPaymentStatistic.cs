namespace PaymentRoutingPoc.Persistence.Models.Read;

/// <summary>
/// Pre-computed analytics for merchant payment processing.
/// Updated through projections to provide fast analytics queries.
/// </summary>
public class MerchantPaymentStatistic
{
    /// <summary>
    /// The merchant ID.
    /// </summary>
    public string MerchantId { get; set; } = null!;

    /// <summary>
    /// The merchant name for display.
    /// </summary>
    public string MerchantName { get; set; } = null!;

    /// <summary>
    /// Total number of payments processed for this merchant.
    /// Includes successful and failed payments.
    /// </summary>
    public int TotalPaymentsProcessed { get; set; }

    /// <summary>
    /// Number of successfully processed payments.
    /// </summary>
    public int SuccessfulPayments { get; set; }

    /// <summary>
    /// Number of failed payments.
    /// </summary>
    public int FailedPayments { get; set; }

    /// <summary>
    /// Total payment volume processed (sum of all amounts).
    /// </summary>
    public decimal TotalVolumeProcessed { get; set; }

    /// <summary>
    /// Average transaction amount.
    /// </summary>
    public decimal? AverageTransactionAmount { get; set; }

    /// <summary>
    /// Success rate as a percentage (0-100).
    /// </summary>
    public decimal? SuccessRate { get; set; }

    /// <summary>
    /// When the most recent payment for this merchant was processed.
    /// </summary>
    public DateTime? LastPaymentAt { get; set; }

    /// <summary>
    /// When these statistics were last updated.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
