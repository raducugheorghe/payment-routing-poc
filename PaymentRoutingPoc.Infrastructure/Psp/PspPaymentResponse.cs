namespace PaymentRoutingPoc.Infrastructure.Psp;

public class PspPaymentResponse
{
    public bool IsSuccess { get; set; }
    public string TransactionId { get; set; } = string.Empty;
    public string? Message { get; set; } = string.Empty;
}