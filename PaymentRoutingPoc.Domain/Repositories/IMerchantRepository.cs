using PaymentRoutingPoc.Domain.Entities;

namespace PaymentRoutingPoc.Domain.Repositories;

public interface IMerchantRepository
{
    Task<Merchant?> GetByIdAsync(Guid merchantId, CancellationToken cancellationToken = default);
}