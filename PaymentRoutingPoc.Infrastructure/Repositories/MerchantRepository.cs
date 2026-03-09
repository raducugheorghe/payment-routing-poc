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
    private readonly ReadDbContext _readDb;

    public MerchantRepository(ReadDbContext readDb)
    {
        _readDb = readDb ?? throw new ArgumentNullException(nameof(readDb));
    }

    public async Task<Merchant?> GetByIdAsync(Guid merchantId, CancellationToken cancellationToken = default)
    {
        var merchantRecord = await _readDb.Merchants
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.MerchantId == merchantId.ToString(), cancellationToken);

        if (merchantRecord == null)
            return null;

        return Merchant.LoadMerchant(merchantId, merchantRecord.Name);
    }
}
