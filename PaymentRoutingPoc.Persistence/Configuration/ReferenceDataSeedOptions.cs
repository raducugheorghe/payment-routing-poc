namespace PaymentRoutingPoc.Persistence.Configuration;

/// <summary>
/// Configurable seed data for reference entities used by local development and tests.
/// </summary>
public class ReferenceDataSeedOptions
{
    public bool Enabled { get; set; }
    public string[] Environments { get; set; } = [];
    public List<SeedCard> Cards { get; set; } = [];
    public List<SeedMerchant> Merchants { get; set; } = [];
}

public class SeedCard
{
    public string CardId { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;
}

public class SeedMerchant
{
    public string MerchantId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
