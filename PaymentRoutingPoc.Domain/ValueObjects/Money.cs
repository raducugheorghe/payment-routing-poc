namespace PaymentRoutingPoc.Domain.ValueObjects;

using ValueOf;

public class Money : ValueOf<(decimal amount, string currency), Money>
{
    public decimal Amount => Value.amount;
    public string Currency => Value.currency.ToUpperInvariant();

    protected override void Validate()
    {
        if (Value.amount < 0)
            throw new ArgumentException("Amount cannot be negative");

        if (string.IsNullOrWhiteSpace(Value.currency))
            throw new ArgumentException("Currency cannot be empty");

        if (Value.currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO code");
    }

    public override string ToString()
    {
        return $"{Amount:F2} {Currency}";
    }
}