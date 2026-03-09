namespace PaymentRoutingPoc.Infrastructure.Repositories;

using Domain.Aggregates;
using Domain.Events;
using Domain.Repositories;
using PaymentRoutingPoc.Persistence.Serialization;

/// <summary>
/// Domain repository backed by the event store.
/// Persists payment state transitions as immutable domain events.
/// </summary>
public class EventSourcedPaymentRepository : IPaymentRepository
{
    private const string AggregateType = "Payment";
    private readonly IEventRepository _eventRepository;

    public EventSourcedPaymentRepository(IEventRepository eventRepository)
    {
        _eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
    }

    public async Task SaveAsync(Payment payment, string? idempotencyKey = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(payment);

        var domainEvents = payment.GetDomainEvents()
            .OfType<IDomainEvent>()
            .ToList();

        if (domainEvents.Count == 0)
            return;

        var expectedVersion = Math.Max(payment.Version - domainEvents.Count, 0);

        await _eventRepository.AppendEventsAsync(
            payment.Id,
            AggregateType,
            domainEvents,
            metadata: new EventMetadata
            {
                IdempotencyKey = idempotencyKey
            },
            expectedVersion: expectedVersion,
            cancellationToken: cancellationToken);

        payment.ClearDomainEvents();

        if (payment.Version > 0 && payment.Version % 100 == 0)
        {
            await _eventRepository.SaveSnapshotAsync(
                payment.Id,
                payment,
                payment.Version,
                cancellationToken);
        }
    }

    public async Task<Payment?> GetByIdAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        if (paymentId == Guid.Empty)
            throw new ArgumentException("Payment ID cannot be empty", nameof(paymentId));

        var snapshotResult = await _eventRepository.GetAggregateWithSnapshotAsync<Payment>(
            paymentId,
            Payment.RehydrateFromEvents,
            cancellationToken);
        if (snapshotResult.HasValue)
            return snapshotResult.Value.aggregate;

        var events = await _eventRepository.GetEventsAsync(paymentId, 0, cancellationToken);
        if (events.Count == 0)
            return null;

        return Payment.RehydrateFromEvents(events);
    }

    public async Task<Payment?> GetByIdempotencyKeyAsync(string idempotencyKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new ArgumentException("Idempotency key cannot be null or empty", nameof(idempotencyKey));

        var aggregateId = await _eventRepository.GetAggregateIdByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (!aggregateId.HasValue)
            return null;

        return await GetByIdAsync(aggregateId.Value, cancellationToken);
    }
}
