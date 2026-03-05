using System.Collections.Concurrent;
using PaymentRoutingPoc.Domain.Entities;

namespace PaymentRoutingPoc.Infrastructure.Repositories;

public class InMemoryRepository<T> where T: EntityBase
{
    protected static readonly ConcurrentDictionary<Guid, T> Store =
        new();

    public Task SaveAsync(T entity, CancellationToken cancellationToken = default)
    {
        if (entity == null)
            throw new ArgumentNullException(nameof(entity));

        Store.AddOrUpdate(entity.Id, entity, (key, existing) => entity);
        return Task.CompletedTask;
    }

    public Task<T?> GetByIdAsync(Guid entityId, CancellationToken cancellationToken = default)
    {
        if (entityId == Guid.Empty)
            throw new ArgumentException($"{typeof(T).Name} ID cannot be empty", nameof(entityId));

        Store.TryGetValue(entityId, out var entity);
        return Task.FromResult(entity);
    }
}