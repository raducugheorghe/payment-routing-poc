using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentRoutingPoc.Domain.Repositories;
using PaymentRoutingPoc.Persistence.DbContexts;
using PaymentRoutingPoc.Persistence.Projections;
using PaymentRoutingPoc.Persistence.Repositories.EventStore;
using PaymentRoutingPoc.Persistence.Repositories.ReadModel;
using PaymentRoutingPoc.Persistence.Serialization;

namespace PaymentRoutingPoc.Persistence.Configuration;

/// <summary>
/// Extension methods for registering persistence layer services.
/// </summary>
public static class PersistenceServiceCollectionExtensions
{
    /// <summary>
    /// Adds persistence layer services to the dependency injection container.
    /// Configures both write model (event store) and read model (CQRS) databases.
    /// </summary>
    public static IServiceCollection AddPersistenceLayer(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Determine database paths
        var baseDirectory = Path.GetDirectoryName(AppContext.BaseDirectory) ?? AppContext.BaseDirectory;
        var writeDbPath = Path.Combine(baseDirectory, "payment-write.db");
        var readDbPath = Path.Combine(baseDirectory, "payment-read.db");

        // Ensure directories exist
        var dbDirectory = Path.GetDirectoryName(writeDbPath);
        if (!string.IsNullOrEmpty(dbDirectory))
        {
            Directory.CreateDirectory(dbDirectory);
        }

        // Register DbContexts
        services.AddDbContext<WriteDbContext>(options =>
        {
            options.UseSqlite(
                $"Data Source={writeDbPath}",
                sqliteOptions =>
                {
                    // Enable Write-Ahead Logging for better concurrent access
                    sqliteOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                });
        });

        services.AddDbContext<ReadDbContext>(options =>
        {
            options.UseSqlite(
                $"Data Source={readDbPath}",
                sqliteOptions =>
                {
                    sqliteOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                });
        });

        // Register Serialization Services
        services.AddSingleton<EventSerializer>();

        // Register Event Sourcing Repository
        services.AddScoped<IEventRepository, EventRepository>();

        // Register Read Repositories
        services.AddScoped<PaymentReadRepository>();
        services.AddScoped<MerchantReadRepository>();

        // Register Projections
        services.AddScoped<IProjection, PaymentProjection>();
        services.AddScoped<IProjection, MerchantStatisticsProjection>();
        services.AddScoped<ProjectionProcessor>();
        services.AddHostedService<ProjectionCatchupBackgroundService>();

        // Register local/dev reference data seeding service.
        services.AddScoped<IReferenceDataSeeder, ReferenceDataSeeder>();

        return services;
    }

    /// <summary>
    /// Initializes the databases (creates or migrates schema).
    /// Should be called during application startup.
    /// </summary>
    public static async Task InitializeDatabasesAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        using var scope = serviceProvider.CreateScope();

        // Migrate read database
        var readDb = scope.ServiceProvider.GetRequiredService<ReadDbContext>();
        await readDb.Database.MigrateAsync(cancellationToken);

        // Migrate write database
        var writeDb = scope.ServiceProvider.GetRequiredService<WriteDbContext>();
        await writeDb.Database.MigrateAsync(cancellationToken);

        // Ensure checkpoint table exists in read model DB even when older migrations are applied.
        await readDb.Database.ExecuteSqlRawAsync(
            @"CREATE TABLE IF NOT EXISTS ProjectionCheckpoints (
                ProjectionId TEXT NOT NULL CONSTRAINT PK_ProjectionCheckpoints PRIMARY KEY,
                LastProcessedGlobalVersion INTEGER NOT NULL DEFAULT 0,
                LastCheckpointTime TEXT NULL,
                ProjectionState TEXT NULL,
                UpdatedAt TEXT NOT NULL DEFAULT (CURRENT_TIMESTAMP)
            );",
            cancellationToken);
    }

    /// <summary>
    /// Processes pending events for all projections and updates the read model.
    /// Should be called periodically (e.g., in a background job or startup).
    /// </summary>
    public static async Task<int> ProcessPendingProjectionsAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        using var scope = serviceProvider.CreateScope();

        var projectionProcessor = scope.ServiceProvider.GetRequiredService<ProjectionProcessor>();
        var projections = scope.ServiceProvider.GetServices<IProjection>();

        return await projectionProcessor.ProcessPendingEventsAsync(projections, cancellationToken);
    }
}
