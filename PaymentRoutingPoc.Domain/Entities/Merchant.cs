namespace PaymentRoutingPoc.Domain.Entities;

public class Merchant
{
    public Guid Id { get; private set; } = Guid.Empty;
    public string Name { get; private set; }

    private Merchant()
    { }
    
    public static Merchant CreateMerchant(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Merchant name cannot be null or empty", nameof(name));

        var merchant = new Merchant
        {
            Id = Guid.NewGuid(),
            Name = name
        };

        return merchant;
    }
    
    public static Merchant LoadMerchant(Guid id, string name)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Merchant ID cannot be empty", nameof(id));
        
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Merchant name cannot be null or empty", nameof(name));

        var merchant = new Merchant
        {
            Id = id,
            Name = name
        };

        return merchant;
    }
}