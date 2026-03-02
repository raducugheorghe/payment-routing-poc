namespace PaymentRoutingPoc.Domain.Entities;

public class Card
{
    // card entity with Id, CardNumber, etc
    public Guid Id { get; private set; }
    public string Pan { get; private set; }
}