using PaymentRoutingPoc.Domain.Entities;
using PaymentRoutingPoc.Domain.Repositories;

namespace PaymentRoutingPoc.Infrastructure.Repositories;

public class InMemoryMerchantRepository : InMemoryRepository<Merchant>, IMerchantRepository
{
    public InMemoryMerchantRepository()
    {
        // Seed with some sample merchant
        var sampleMerchant = Merchant.LoadMerchant(Guid.AllBitsSet,"Sample Merchant");
        Store.TryAdd(sampleMerchant.Id, sampleMerchant);
    }
}