using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PaymentRoutingPoc.Domain.Events;
using PaymentRoutingPoc.Persistence.DbContexts;
using PaymentRoutingPoc.Persistence.Models.Read;

namespace PaymentRoutingPoc.Persistence.Projections;

/// <summary>
/// Projects payment events into merchant statistics for analytics.
/// Aggregates payments per merchant and maintains success rates, volumes, etc.
/// </summary>
public class MerchantStatisticsProjection : IProjection
{
    public string ProjectionId => nameof(MerchantStatisticsProjection);

    private readonly ReadDbContext _readDb;
    private readonly ILogger<MerchantStatisticsProjection> _logger;

    public MerchantStatisticsProjection(ReadDbContext readDb, ILogger<MerchantStatisticsProjection> logger)
    {
        _readDb = readDb ?? throw new ArgumentNullException(nameof(readDb));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        try
        {
            switch (domainEvent)
            {
                // Only care about final payment outcomes for statistics
                case PaymentSucceededEvent succeeded:
                    await HandlePaymentSucceededAsync(succeeded, cancellationToken);
                    break;

                case PaymentFailedEvent failed:
                    await HandlePaymentFailedAsync(failed, cancellationToken);
                    break;

                default:
                    // Ignore PaymentSubmittedEvent as it doesn't represent a final outcome
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error handling event {EventType} in MerchantStatisticsProjection",
                domainEvent.GetType().Name);
            throw;
        }
    }

    private async Task HandlePaymentSucceededAsync(
        PaymentSucceededEvent @event,
        CancellationToken cancellationToken)
    {
        // Load or create payment to get merchant info
        var payment = await _readDb.PaymentsReadModel
            .FirstOrDefaultAsync(p => p.PaymentId == @event.PaymentId.ToString(), cancellationToken);

        if (payment == null)
        {
            _logger.LogWarning(
                "PaymentReadModel not found when processing PaymentSucceededEvent for payment {PaymentId}",
                @event.PaymentId);
            return;
        }

        // Get or create merchant statistics
        var merchantId = payment.MerchantId;
        var stats = await _readDb.MerchantPaymentStatistics
            .FirstOrDefaultAsync(m => m.MerchantId == merchantId, cancellationToken);

        if (stats == null)
        {
            stats = new MerchantPaymentStatistic
            {
                MerchantId = merchantId,
                MerchantName = payment.MerchantName,
                TotalPaymentsProcessed = 0,
                SuccessfulPayments = 0,
                FailedPayments = 0,
                TotalVolumeProcessed = 0,
                UpdatedAt = DateTime.UtcNow
            };
            _readDb.MerchantPaymentStatistics.Add(stats);
        }

        // Update statistics
        stats.TotalPaymentsProcessed++;
        stats.SuccessfulPayments++;
        stats.TotalVolumeProcessed += @event.Amount;
        stats.LastPaymentAt = @event.OccurredAt;
        stats.UpdatedAt = DateTime.UtcNow;

        // Recalculate derived values
        RecalculateStatistics(stats);

        _logger.LogInformation(
            "Updated merchant statistics for {MerchantId}. Success rate: {SuccessRate}%",
            merchantId,
            stats.SuccessRate);
    }

    private async Task HandlePaymentFailedAsync(
        PaymentFailedEvent @event,
        CancellationToken cancellationToken)
    {
        // Load payment to get merchant info
        var payment = await _readDb.PaymentsReadModel
            .FirstOrDefaultAsync(p => p.PaymentId == @event.PaymentId.ToString(), cancellationToken);

        if (payment == null)
        {
            _logger.LogWarning(
                "PaymentReadModel not found when processing PaymentFailedEvent for payment {PaymentId}",
                @event.PaymentId);
            return;
        }

        // Get or create merchant statistics
        var merchantId = payment.MerchantId;
        var stats = await _readDb.MerchantPaymentStatistics
            .FirstOrDefaultAsync(m => m.MerchantId == merchantId, cancellationToken);

        if (stats == null)
        {
            stats = new MerchantPaymentStatistic
            {
                MerchantId = merchantId,
                MerchantName = payment.MerchantName,
                TotalPaymentsProcessed = 0,
                SuccessfulPayments = 0,
                FailedPayments = 0,
                TotalVolumeProcessed = 0,
                UpdatedAt = DateTime.UtcNow
            };
            _readDb.MerchantPaymentStatistics.Add(stats);
        }

        // Update statistics
        stats.TotalPaymentsProcessed++;
        stats.FailedPayments++;
        stats.LastPaymentAt = @event.OccurredAt;
        stats.UpdatedAt = DateTime.UtcNow;

        // Recalculate derived values
        RecalculateStatistics(stats);

        _logger.LogInformation(
            "Updated merchant statistics for {MerchantId} after payment failure. Success rate: {SuccessRate}%",
            merchantId,
            stats.SuccessRate);
    }

    private void RecalculateStatistics(MerchantPaymentStatistic stats)
    {
        // Calculate average transaction amount
        if (stats.TotalPaymentsProcessed > 0)
        {
            stats.AverageTransactionAmount = stats.SuccessfulPayments > 0
                ? stats.TotalVolumeProcessed / stats.SuccessfulPayments
                : 0;

            // Calculate success rate
            stats.SuccessRate = (decimal)stats.SuccessfulPayments / stats.TotalPaymentsProcessed * 100;
        }
        else
        {
            stats.AverageTransactionAmount = 0;
            stats.SuccessRate = 0;
        }
    }
}
