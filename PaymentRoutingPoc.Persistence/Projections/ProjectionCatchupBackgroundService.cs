using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PaymentRoutingPoc.Persistence.Projections;

/// <summary>
/// Periodically catches up projections to reduce read-model lag when no events are currently flowing.
/// </summary>
public sealed class ProjectionCatchupBackgroundService : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProjectionCatchupBackgroundService> _logger;

    public ProjectionCatchupBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<ProjectionCatchupBackgroundService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Run one immediate catch-up on startup.
        await CatchUpAsync(stoppingToken);

        using var timer = new PeriodicTimer(PollInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await CatchUpAsync(stoppingToken);
        }
    }

    private async Task CatchUpAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceProvider.CreateScope();
            var processor = scope.ServiceProvider.GetRequiredService<ProjectionProcessor>();
            var projections = scope.ServiceProvider.GetServices<IProjection>();

            var processed = await processor.ProcessPendingEventsAsync(projections, cancellationToken);
            if (processed > 0)
            {
                _logger.LogInformation("Projection catch-up processed {Count} events", processed);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Graceful shutdown.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Projection catch-up iteration failed");
        }
    }
}
