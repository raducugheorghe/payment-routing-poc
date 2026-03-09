using Microsoft.EntityFrameworkCore;
using PaymentRoutingPoc.Persistence.DbContexts;
using PaymentRoutingPoc.Persistence.Models.Read;

namespace PaymentRoutingPoc.Persistence.Repositories.ReadModel;

/// <summary>
/// Read repository for querying merchant payment statistics.
/// Provides fast analytics and KPI queries.
/// </summary>
public class MerchantReadRepository
{
    private readonly ReadDbContext _readDb;

    public MerchantReadRepository(ReadDbContext readDb)
    {
        _readDb = readDb ?? throw new ArgumentNullException(nameof(readDb));
    }

    /// <summary>
    /// Gets statistics for a specific merchant.
    /// </summary>
    public async Task<MerchantPaymentStatistic?> GetMerchantStatisticsAsync(
        Guid merchantId,
        CancellationToken cancellationToken = default)
    {
        var merchantIdString = merchantId.ToString();
        return await _readDb.MerchantPaymentStatistics
            .FirstOrDefaultAsync(m => m.MerchantId == merchantIdString, cancellationToken);
    }

    /// <summary>
    /// Gets all merchant statistics, ordered by total volume.
    /// </summary>
    public async Task<List<MerchantPaymentStatistic>> GetAllMerchantStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        return await _readDb.MerchantPaymentStatistics
            .OrderByDescending(m => m.TotalVolumeProcessed)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets top N merchants by transaction volume.
    /// </summary>
    public async Task<List<MerchantPaymentStatistic>> GetTopMerchantsByVolumeAsync(
        int topCount = 10,
        CancellationToken cancellationToken = default)
    {
        return await _readDb.MerchantPaymentStatistics
            .OrderByDescending(m => m.TotalVolumeProcessed)
            .Take(topCount)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets top N merchants by success rate.
    /// </summary>
    public async Task<List<MerchantPaymentStatistic>> GetTopMerchantsBySuccessRateAsync(
        int topCount = 10,
        CancellationToken cancellationToken = default)
    {
        return await _readDb.MerchantPaymentStatistics
            .Where(m => m.SuccessRate.HasValue)
            .OrderByDescending(m => m.SuccessRate)
            .Take(topCount)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets merchants with success rate below a threshold (at-risk merchants).
    /// </summary>
    public async Task<List<MerchantPaymentStatistic>> GetMerchantsBelowSuccessRateAsync(
        decimal thresholdPercentage,
        CancellationToken cancellationToken = default)
    {
        return await _readDb.MerchantPaymentStatistics
            .Where(m => m.SuccessRate.HasValue && m.SuccessRate < thresholdPercentage)
            .OrderBy(m => m.SuccessRate)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets merchants with most recent payment activity.
    /// </summary>
    public async Task<List<MerchantPaymentStatistic>> GetActiveeMerchantsAsync(
        int daysSinceLastPayment = 30,
        CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysSinceLastPayment);

        return await _readDb.MerchantPaymentStatistics
            .Where(m => m.LastPaymentAt.HasValue && m.LastPaymentAt >= cutoffDate)
            .OrderByDescending(m => m.LastPaymentAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets merchants with payment activity in a specific period.
    /// </summary>
    public async Task<List<MerchantPaymentStatistic>> GetMerchantsWithActivityInPeriodAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _readDb.MerchantPaymentStatistics
            .Where(m => m.LastPaymentAt.HasValue &&
                        m.LastPaymentAt >= startDate &&
                        m.LastPaymentAt <= endDate)
            .OrderByDescending(m => m.LastPaymentAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets aggregate statistics across all merchants.
    /// </summary>
    public async Task<AggregateStatistics?> GetAggregateStatisticsAsync(
        CancellationToken cancellationToken = default)
    {
        var stats = await _readDb.MerchantPaymentStatistics.ToListAsync(cancellationToken);

        if (stats.Count == 0)
            return null;

        return new AggregateStatistics
        {
            TotalMerchants = stats.Count,
            TotalPaymentsProcessed = stats.Sum(m => m.TotalPaymentsProcessed),
            TotalSuccessfulPayments = stats.Sum(m => m.SuccessfulPayments),
            TotalFailedPayments = stats.Sum(m => m.FailedPayments),
            TotalVolume = stats.Sum(m => m.TotalVolumeProcessed),
            AverageSuccessRate = stats.Average(m => m.SuccessRate ?? 0),
            AverageTransactionAmount = stats.Average(m => m.AverageTransactionAmount ?? 0)
        };
    }
}

/// <summary>
/// Aggregate statistics across all merchants.
/// </summary>
public class AggregateStatistics
{
    public int TotalMerchants { get; set; }
    public int TotalPaymentsProcessed { get; set; }
    public int TotalSuccessfulPayments { get; set; }
    public int TotalFailedPayments { get; set; }
    public decimal TotalVolume { get; set; }
    public decimal AverageSuccessRate { get; set; }
    public decimal AverageTransactionAmount { get; set; }

    public decimal OverallSuccessRate =>
        TotalPaymentsProcessed > 0
            ? (decimal)TotalSuccessfulPayments / TotalPaymentsProcessed * 100
            : 0;
}
