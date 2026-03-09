namespace PaymentRoutingPoc.Persistence.Models.Read;

/// <summary>
/// Reference merchant data used by command-side validation.
/// </summary>
public class MerchantRecord
{
    public string MerchantId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
