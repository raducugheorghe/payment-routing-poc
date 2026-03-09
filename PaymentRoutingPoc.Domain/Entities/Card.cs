namespace PaymentRoutingPoc.Domain.Entities;

public class Card
{
    // card entity with Id, CardNumber, etc
    public Guid Id { get; private set; } = Guid.Empty;
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
    
    public static Card LoadCard(Guid id, string cardNumber)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Card ID cannot be empty", nameof(id));
        
        if (string.IsNullOrWhiteSpace(cardNumber))
            throw new ArgumentException("Card number cannot be null or empty", nameof(cardNumber));

        var card = new Card
        {
            Id = id,
            CardNumber = cardNumber
        };

        return card;
    }

    public string GetLast4()
    {
        var digits = new string(CardNumber.Where(char.IsDigit).ToArray());
        if (digits.Length < 4)
            throw new InvalidOperationException("Card number must contain at least 4 digits");

        return digits[^4..];
    }

    public static string BuildMaskedFromLast4(string last4)
    {
        if (string.IsNullOrWhiteSpace(last4) || last4.Length != 4 || !last4.All(char.IsDigit))
            throw new ArgumentException("Card last4 must be exactly 4 digits", nameof(last4));

        return $"**** **** **** {last4}";
    }
}