using Microsoft.EntityFrameworkCore;
using PaymentRoutingPoc.Persistence.Models.Write;

namespace PaymentRoutingPoc.Persistence.DbContexts;

/// <summary>
/// DbContext for the write model (Event Store).
/// Stores immutable events, snapshots, and projection checkpoints.
/// Optimized for event appending and snapshot retrieval.
/// </summary>
public class WriteDbContext : DbContext
{
    public WriteDbContext(DbContextOptions<WriteDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// The event store table containing all domain events.
    /// </summary>
    public DbSet<StoredEvent> Events { get; set; } = null!;

    /// <summary>
    /// Snapshots for aggregate optimization.
    /// </summary>
    public DbSet<Snapshot> Snapshots { get; set; } = null!;

    /// <summary>
    /// Card reference data used for command validation.
    /// </summary>
    public DbSet<CardRecord> Cards { get; set; } = null!;

    /// <summary>
    /// Merchant reference data used for command validation.
    /// </summary>
    public DbSet<MerchantRecord> Merchants { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure StoredEvent
        modelBuilder.Entity<StoredEvent>(entity =>
        {
            entity.HasKey(e => e.EventId);

            entity.Property(e => e.EventId)
                .HasMaxLength(36)
                .IsRequired();

            entity.Property(e => e.EventStreamId)
                .HasMaxLength(36)
                .IsRequired();

            entity.Property(e => e.AggregateType)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.EventType)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.EventData)
                .IsRequired();

            entity.Property(e => e.Metadata)
                .IsRequired();

            entity.Property(e => e.IdempotencyKey)
                .HasMaxLength(200)
                .IsRequired(false);

            entity.Property(e => e.OccurredAt)
                .IsRequired();

            entity.Property(e => e.RecordedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.IsCommitted)
                .HasDefaultValue(true);

            // Indexes for efficient querying
            entity.HasIndex(e => new { e.EventStreamId, e.AggregateVersion })
                .IsUnique()
                .HasDatabaseName("idx_stream_id_version");

            entity.HasIndex(e => e.GlobalVersion)
                .IsUnique()
                .HasDatabaseName("idx_global_version");

            entity.HasIndex(e => e.EventType)
                .HasDatabaseName("idx_event_type");

            entity.HasIndex(e => e.IdempotencyKey)
                .IsUnique()
                .HasDatabaseName("idx_idempotency_key");

            entity.HasIndex(e => e.RecordedAt)
                .HasDatabaseName("idx_recorded_at");
        });

        // Configure Snapshot
        modelBuilder.Entity<Snapshot>(entity =>
        {
            entity.HasKey(e => e.SnapshotId);

            entity.Property(e => e.SnapshotId)
                .HasMaxLength(36)
                .IsRequired();

            entity.Property(e => e.EventStreamId)
                .HasMaxLength(36)
                .IsRequired();

            entity.Property(e => e.AggregateType)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.AggregateData)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired();

            // Unique index to ensure only one snapshot per aggregate
            entity.HasIndex(e => new { e.EventStreamId, e.AggregateType })
                .IsUnique()
                .HasDatabaseName("idx_snapshot_stream_aggregate");

            entity.HasIndex(e => e.EventStreamId)
                .HasDatabaseName("idx_snapshot_stream_id");
        });

        // Configure CardRecord
        modelBuilder.Entity<CardRecord>(entity =>
        {
            entity.ToTable("Cards");

            entity.HasKey(e => e.CardId);

            entity.Property(e => e.CardId)
                .HasMaxLength(36)
                .IsRequired();

            entity.Property(e => e.CardNumber)
                .HasMaxLength(19)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.CardNumber)
                .IsUnique()
                .HasDatabaseName("idx_cards_number");
        });

        // Configure MerchantRecord
        modelBuilder.Entity<MerchantRecord>(entity =>
        {
            entity.ToTable("Merchants");

            entity.HasKey(e => e.MerchantId);

            entity.Property(e => e.MerchantId)
                .HasMaxLength(36)
                .IsRequired();

            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasIndex(e => e.Name)
                .HasDatabaseName("idx_merchants_name");
        });
    }
}
