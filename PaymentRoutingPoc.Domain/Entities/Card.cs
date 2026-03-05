namespace PaymentRoutingPoc.Domain.Entities;

public class Card : EntityBase
{
    // card entity with Id, CardNumber, etc
    public string CardNumber { get; private set; }

    private Card() {
    }
    
    public static Card CreateCard(string cardNumber)
    {
        if (string.IsNullOrWhiteSpace(cardNumber))
            throw new ArgumentException("Card number cannot be null or empty", nameof(cardNumber));

        var card = new Card
        {
            Id = Guid.NewGuid(),
            CardNumber = cardNumber
        };

        return card;
    }
}