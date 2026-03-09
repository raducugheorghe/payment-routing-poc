using Microsoft.EntityFrameworkCore;
using PaymentRoutingPoc.Persistence.DbContexts;
using PaymentRoutingPoc.Persistence.Models.Read;

namespace PaymentRoutingPoc.Persistence.Repositories.ReadModel;

/// <summary>
/// Read repository for querying payment data from the denormalized read model.
/// Optimized for fast queries and analytics.
/// </summary>
public class PaymentReadRepository
{
    private readonly ReadDbContext _readDb;

    public PaymentReadRepository(ReadDbContext readDb)
    {
        _readDb = readDb ?? throw new ArgumentNullException(nameof(readDb));
    }

    /// <summary>
    /// Gets a payment by ID.
    /// </summary>
    public async Task<PaymentReadModel?> GetPaymentByIdAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var paymentIdString = paymentId.ToString();
        return await _readDb.PaymentsReadModel
            .FirstOrDefaultAsync(p => p.PaymentId == paymentIdString, cancellationToken);
    }

    /// <summary>
    /// Gets all payments with a specific status.
    /// </summary>
    public async Task<List<PaymentReadModel>> GetPaymentsByStatusAsync(
        string status,
        CancellationToken cancellationToken = default)
    {
        return await _readDb.PaymentsReadModel
            .Where(p => p.Status == status)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all payments for a specific merchant.
    /// </summary>
    public async Task<List<PaymentReadModel>> GetPaymentsByMerchantAsync(
        Guid merchantId,
        CancellationToken cancellationToken = default)
    {
        var merchantIdString = merchantId.ToString();
        return await _readDb.PaymentsReadModel
            .Where(p => p.MerchantId == merchantIdString)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets payments for a merchant with a specific status.
    /// </summary>
    public async Task<List<PaymentReadModel>> GetPaymentsByMerchantAndStatusAsync(
        Guid merchantId,
        string status,
        CancellationToken cancellationToken = default)
    {
        var merchantIdString = merchantId.ToString();
        return await _readDb.PaymentsReadModel
            .Where(p => p.MerchantId == merchantIdString && p.Status == status)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets event history for a payment (audit trail).
    /// </summary>
    public async Task<List<PaymentEventLog>> GetPaymentEventHistoryAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        var paymentIdString = paymentId.ToString();
        return await _readDb.PaymentEventLogs
            .Where(e => e.PaymentId == paymentIdString)
            .OrderBy(e => e.OccurredAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets paginated list of payments, ordered by most recent.
    /// </summary>
    public async Task<(List<PaymentReadModel> items, int totalCount)> GetPaymentsPaginatedAsync(
        int pageNumber = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (pageNumber < 1) pageNumber = 1;
        if (pageSize < 1 || pageSize > 1000) pageSize = 50;

        var query = _readDb.PaymentsReadModel.OrderByDescending(p => p.CreatedAt);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    /// <summary>
    /// Gets payments created within a date range.
    /// </summary>
    public async Task<List<PaymentReadModel>> GetPaymentsByDateRangeAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        return await _readDb.PaymentsReadModel
            .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Gets payment statistics (count by currency and status).
    /// </summary>
    public async Task<Dictionary<string, int>> GetPaymentCountByCurrencyAsync(
        CancellationToken cancellationToken = default)
    {
        return await _readDb.PaymentsReadModel
            .GroupBy(p => p.Currency)
            .Select(g => new { currency = g.Key, count = g.Count() })
            .ToListAsync(cancellationToken)
            .ContinueWith(task => task.Result.ToDictionary(x => x.currency, x => x.count));
    }

    /// <summary>
    /// Gets total payment volume by currency.
    /// </summary>
    public async Task<Dictionary<string, decimal>> GetTotalVolumeByStatusAsync(
        CancellationToken cancellationToken = default)
    {
        return await _readDb.PaymentsReadModel
            .GroupBy(p => p.Status)
            .Select(g => new { status = g.Key, volume = g.Sum(p => p.Amount) })
            .ToListAsync(cancellationToken)
            .ContinueWith(task => task.Result.ToDictionary(x => x.status, x => x.volume));
    }
}
