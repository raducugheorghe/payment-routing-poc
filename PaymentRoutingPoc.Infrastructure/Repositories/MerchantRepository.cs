namespace PaymentRoutingPoc.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Repositories;
using PaymentRoutingPoc.Persistence.DbContexts;

/// <summary>
/// Repository for merchant reference data used by command validation.
/// </summary>
public class MerchantRepository : IMerchantRepository
{
    private readonly WriteDbContext _writeDb;

    public MerchantRepository(WriteDbContext writeDb)
    {
        _writeDb = writeDb ?? throw new ArgumentNullException(nameof(writeDb));
    }

    public async Task<Merchant?> GetByIdAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        var merchantRecord = await _writeDb.Merchants
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MerchantId == merchantId.ToString(), cancellationToken);

        if (merchantRecord == null)
            return null;

        return Merchant.LoadMerchant(merchantId, merchantRecord.Name);
    }
}
