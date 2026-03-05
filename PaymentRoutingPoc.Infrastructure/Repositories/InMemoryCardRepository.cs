using PaymentRoutingPoc.Domain.Entities;
using PaymentRoutingPoc.Domain.Repositories;

namespace PaymentRoutingPoc.Infrastructure.Repositories;

public class InMemoryCardRepository : InMemoryRepository<Card>, ICardRepository
{
    public InMemoryCardRepository()
    {
        // Seed with some sample card
        var sampleCard = Card.CreateCard("4111111111111111");
        Store.TryAdd(sampleCard.Id, sampleCard);
    }
    
    public Task<Card?> GetByCardNumberAsync(string cardNumber, CancellationToken cancellationToken = default)
    {
        var card = Store.Values.FirstOrDefault(c => c.CardNumber == cardNumber);
        return Task.FromResult(card);
    }
}