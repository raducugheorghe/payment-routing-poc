namespace PaymentRoutingPoc.Persistence.Models.Read;

/// <summary>
/// Reference card data used by command-side validation.
/// </summary>
public class CardRecord
{
    public string CardId { get; set; } = null!;
    public string CardNumber { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
