namespace PaymentRoutingPoc.Domain.Repositories;

using Aggregates;

public interface IPaymentRepository
{
    Task SaveAsync(Payment payment, CancellationToken cancellationToken = default);
    Task<Payment?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken = default);
}

