using Microsoft.EntityFrameworkCore;
using PaymentRoutingPoc.Persistence.Models.Read;

namespace PaymentRoutingPoc.Persistence.DbContexts;

/// <summary>
/// DbContext for the read model (CQRS).
/// Contains denormalized views optimized for queries.
/// Updated by projections in response to domain events.
/// </summary>
public class ReadDbContext : DbContext
{
    public ReadDbContext(DbContextOptions<ReadDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Denormalized payment read model optimized for fast queries.
    /// </summary>
    public DbSet<PaymentReadModel> PaymentsReadModel { get; set; } = null!;

    /// <summary>
    /// Audit trail of payment events for compliance and debugging.
    /// </summary>
    public DbSet<PaymentEventLog> PaymentEventLogs { get; set; } = null!;

    /// <summary>
    /// Pre-computed merchant payment statistics for analytics.
    /// </summary>
    public DbSet<MerchantPaymentStatistic> MerchantPaymentStatistics { get; set; } = null!;

    /// <summary>
    /// Projection checkpoints for idempotent event processing.
    /// </summary>
    public DbSet<ProjectionCheckpoint> ProjectionCheckpoints { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure PaymentReadModel
        modelBuilder.Entity<PaymentReadModel>(entity =>
        {
            entity.HasKey(e => e.PaymentId);

            entity.Property(e => e.PaymentId)
                .HasMaxLength(36)
                .IsRequired();

            entity.Property(e => e.Amount)
                .HasPrecision(18, 2)
                .IsRequired();

            entity.Property(e => e.Currency)
                .HasMaxLength(3)
                .IsRequired();

            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.CardId)
                .HasMaxLength(36)
                .IsRequired();

            entity.Property(e => e.CardNumber)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.MerchantId)
                .HasMaxLength(36)
                .IsRequired();

            entity.Property(e => e.MerchantName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.ProviderTransactionId)
                .HasMaxLength(100);

            entity.Property(e => e.ProviderName)
                .HasMaxLength(100);

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            entity.Property(e => e.ProcessedAt)
                .IsRequired(false);

            entity.Property(e => e.FailureReason)
                .HasMaxLength(500);

            entity.Property(e => e.LastEventType)
                .HasMaxLength(100);

            // Indexes optimized for common queries
            entity.HasIndex(e => e.Status)
                .HasDatabaseName("idx_payment_status");

            entity.HasIndex(e => e.MerchantId)
                .HasDatabaseName("idx_payment_merchant_id");

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("idx_payment_created_at");

            entity.HasIndex(e => new { e.Currency, e.Status })
                .HasDatabaseName("idx_payment_currency_status");

            entity.HasIndex(e => e.Amount)
                .HasDatabaseName("idx_payment_amount");
        });

        // Configure PaymentEventLog
        modelBuilder.Entity<PaymentEventLog>(entity =>
        {
            entity.HasKey(e => e.EventLogId);

            entity.Property(e => e.EventLogId)
                .HasMaxLength(36)
                .IsRequired();

            entity.Property(e => e.PaymentId)
                .HasMaxLength(36)
                .IsRequired();

            entity.Property(e => e.EventType)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.EventData)
                .IsRequired();

            entity.Property(e => e.OccurredAt)
                .IsRequired();

            // Indexes for efficient audit queries
            entity.HasIndex(e => e.PaymentId)
                .HasDatabaseName("idx_event_log_payment_id");

            entity.HasIndex(e => e.OccurredAt)
                .HasDatabaseName("idx_event_log_occurred_at");

            // Soft foreign key relationship (payment may be deleted)
            entity.HasIndex(e => new { e.PaymentId, e.OccurredAt })
                .HasDatabaseName("idx_event_log_payment_occurred");
        });

        // Configure MerchantPaymentStatistic
        modelBuilder.Entity<MerchantPaymentStatistic>(entity =>
        {
            entity.HasKey(e => e.MerchantId);

            entity.Property(e => e.MerchantId)
                .HasMaxLength(36)
                .IsRequired();

            entity.Property(e => e.MerchantName)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.TotalPaymentsProcessed)
                .HasDefaultValue(0)
                .IsRequired();

            entity.Property(e => e.SuccessfulPayments)
                .HasDefaultValue(0)
                .IsRequired();

            entity.Property(e => e.FailedPayments)
                .HasDefaultValue(0)
                .IsRequired();

            entity.Property(e => e.TotalVolumeProcessed)
                .HasPrecision(18, 2)
                .HasDefaultValue(0)
                .IsRequired();

            entity.Property(e => e.AverageTransactionAmount)
                .HasPrecision(18, 2);

            entity.Property(e => e.SuccessRate)
                .HasPrecision(5, 2);

            entity.Property(e => e.LastPaymentAt)
                .IsRequired(false);

            entity.Property(e => e.UpdatedAt)
                .IsRequired(false);

            // Index for analytics queries
            entity.HasIndex(e => e.MerchantId)
                .HasDatabaseName("idx_merchant_statistic_id");
        });

        modelBuilder.Entity<ProjectionCheckpoint>(entity =>
        {
            entity.HasKey(e => e.ProjectionId);

            entity.Property(e => e.ProjectionId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.LastCheckpointTime)
                .IsRequired(false);

            entity.Property(e => e.ProjectionState)
                .IsRequired(false);

            entity.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        });
    }
}
