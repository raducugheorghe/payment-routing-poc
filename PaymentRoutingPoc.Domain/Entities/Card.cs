namespace PaymentRoutingPoc.Domain.Entities;

public class Card
{
    // card entity with Id, CardNumber, etc
    public Guid Id { get; private set; }
    public string Pan { get; private set; }

    private Card() {
    }
    
    public static Card CreateCard(string pan)
    {
        if (string.IsNullOrWhiteSpace(pan))
            throw new ArgumentException("PAN cannot be null or empty", nameof(pan));

        var card = new Card
        {
            Id = Guid.NewGuid(),
            Pan = pan
        };

        return card;
    }
}