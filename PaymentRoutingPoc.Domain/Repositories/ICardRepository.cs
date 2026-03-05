using PaymentRoutingPoc.Domain.Entities;

namespace PaymentRoutingPoc.Domain.Repositories;

public interface ICardRepository
{
    Task<Card?> GetByCardNumberAsync(string cardNumber, CancellationToken cancellationToken = default);
}