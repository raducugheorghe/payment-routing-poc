namespace PaymentRoutingPoc.Application.Services;

using Domain.Aggregates;
using DTOs;

public interface IPaymentOrchestrator
{
    Task<PaymentOrchestratorResult> ExecuteWithFallbackAsync(
        Payment payment,
        CancellationToken cancellationToken = default);
}