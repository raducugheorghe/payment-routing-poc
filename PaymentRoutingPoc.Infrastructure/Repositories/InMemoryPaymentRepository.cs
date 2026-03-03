namespace PaymentRoutingPoc.Infrastructure.Repositories;

using System.Collections.Concurrent;
using Domain.Aggregates;
using Domain.Repositories;

public class InMemoryPaymentRepository : IPaymentRepository
{
    private static readonly ConcurrentDictionary<Guid, Payment> PaymentStore =
        new();

    public Task SaveAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        if (payment == null)
            throw new ArgumentNullException(nameof(payment));

        PaymentStore.AddOrUpdate(payment.Id, payment, (key, existing) => payment);
        return Task.CompletedTask;
    }

    public Task<Payment?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        if (paymentId == Guid.Empty)
            throw new ArgumentException("Payment ID cannot be empty", nameof(paymentId));

        PaymentStore.TryGetValue(paymentId, out var payment);
        return Task.FromResult(payment);
    }
}

