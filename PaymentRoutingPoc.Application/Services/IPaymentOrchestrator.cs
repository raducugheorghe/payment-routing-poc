namespace PaymentRoutingPoc.Application.Services;

public interface IPaymentOrchestrator
{
    Task<PaymentOrchestratorResult> ExecuteWithFallbackAsync(
        decimal amount,
        string currency,
        CancellationToken cancellationToken = default);
}

public class PaymentOrchestratorResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
}

