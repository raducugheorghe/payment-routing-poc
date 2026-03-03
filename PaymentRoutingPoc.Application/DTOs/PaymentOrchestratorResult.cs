namespace PaymentRoutingPoc.Application.DTOs;

public class PaymentOrchestratorResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ProviderTransactionId { get; set; }
}