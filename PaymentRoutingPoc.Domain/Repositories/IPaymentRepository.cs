namespace PaymentRoutingPoc.Domain.Repositories;

using Aggregates;

public interface IPaymentRepository
{
    Task SaveAsync(Payment payment, string? idempotencyKey = null, CancellationToken cancellationToken = default);
    Task<Payment?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task<Payment?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default);
}