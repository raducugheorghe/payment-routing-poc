namespace PaymentRoutingPoc.Application.DTOs;

public class CreatePaymentRequest
{
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string CardNumber { get; set; } = string.Empty;
    public string MerchantId { get; set; } = string.Empty;
}

