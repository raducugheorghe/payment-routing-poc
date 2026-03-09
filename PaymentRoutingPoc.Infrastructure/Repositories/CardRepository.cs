namespace PaymentRoutingPoc.Infrastructure.Repositories;

using Microsoft.EntityFrameworkCore;
using Domain.Entities;
using Domain.Repositories;
using PaymentRoutingPoc.Persistence.DbContexts;

/// <summary>
/// Repository for card reference data used by command validation.
/// </summary>
public class CardRepository : ICardRepository
{
    private readonly ReadDbContext _readDb;

    public CardRepository(ReadDbContext readDb)
    {
        _readDb = readDb ?? throw new ArgumentNullException(nameof(readDb));
    }

    public async Task<Card?> GetByCardNumberAsync(string cardNumber, CancellationToken cancellationToken = default)
    {
        var cardRecord = await _readDb.Cards
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CardNumber == cardNumber, cancellationToken);

        if (cardRecord == null)
            return null;

        return Card.LoadCard(Guid.Parse(cardRecord.CardId), cardRecord.CardNumber);
    }
}
