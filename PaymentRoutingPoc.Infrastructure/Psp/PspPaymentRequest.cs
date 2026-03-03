namespace PaymentRoutingPoc.Infrastructure.Psp;

public class PspPaymentRequest
{
    public decimal Amount { get; set; }
    public string  Currency { get; set; }
    public string CardNumber { get; set; }
}