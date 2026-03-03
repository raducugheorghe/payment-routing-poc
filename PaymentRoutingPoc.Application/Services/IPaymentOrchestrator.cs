using PaymentRoutingPoc.Application.DTOs;
using PaymentRoutingPoc.Domain.Aggregates;

namespace PaymentRoutingPoc.Application.Services;

public interface IPaymentOrchestrator
{
    Task<PaymentOrchestratorResult> ExecuteWithFallbackAsync(
        Payment payment,
        CancellationToken cancellationToken = default);
}