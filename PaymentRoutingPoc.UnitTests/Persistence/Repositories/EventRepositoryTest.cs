using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PaymentRoutingPoc.Domain.Aggregates;
using PaymentRoutingPoc.Domain.Entities;
using PaymentRoutingPoc.Domain.Events;
using PaymentRoutingPoc.Domain.ValueObjects;
using PaymentRoutingPoc.Persistence.DbContexts;
using PaymentRoutingPoc.Persistence.Repositories.EventStore;
using PaymentRoutingPoc.Persistence.Serialization;

namespace PaymentRoutingPoc.UnitTests.Persistence.Repositories;

public class EventRepositoryTest
{
    [Fact]
    public async Task AppendAndGetEvents_ShouldPreserveOrder()
    {
        using var connection = CreateInMemoryConnection();
        await using var db = CreateWriteDb(connection);

        var repository = new EventRepository(db, new EventSerializer(), NullLogger<EventRepository>.Instance);

        var payment = BuildPayment();
        payment.Submit();
        payment.MarkAsFailed("declined");

        var events = payment.GetDomainEvents().OfType<IDomainEvent>().ToList();

        await repository.AppendEventsAsync(
            payment.Id,
            "Payment",
            events,
            new EventMetadata { IdempotencyKey = Guid.NewGuid().ToString("N") },
            expectedVersion: 0);

        var loaded = await repository.GetEventsAsync(payment.Id);

        Assert.Equal(2, loaded.Count);
        Assert.IsType<PaymentSubmittedEvent>(loaded[0]);
        Assert.IsType<PaymentFailedEvent>(loaded[1]);
        Assert.Equal(1, loaded[0].AggregateVersion);
        Assert.Equal(2, loaded[1].AggregateVersion);
    }

    [Fact]
    public async Task AppendEvents_WithWrongExpectedVersion_ShouldThrowConcurrencyException()
    {
        using var connection = CreateInMemoryConnection();
        await using var db = CreateWriteDb(connection);

        var repository = new EventRepository(db, new EventSerializer(), NullLogger<EventRepository>.Instance);

        var payment = BuildPayment();
        payment.Submit();

        await repository.AppendEventsAsync(
            payment.Id,
            "Payment",
            payment.GetDomainEvents().OfType<IDomainEvent>().ToList(),
            expectedVersion: 0);

        var conflictingEvent = new PaymentSucceededEvent(
            payment.Id,
            payment.Total.Amount,
            payment.Total.Currency,
            "tx-conflict",
            "PSP-Conflict",
            2);

        await Assert.ThrowsAsync<ConcurrencyException>(() => repository.AppendEventsAsync(
            payment.Id,
            "Payment",
            [conflictingEvent],
            expectedVersion: 0));
    }

    [Fact]
    public async Task GetAggregateWithSnapshot_ShouldReplayEventsAfterSnapshot()
    {
        using var connection = CreateInMemoryConnection();
        await using var db = CreateWriteDb(connection);

        var repository = new EventRepository(db, new EventSerializer(), NullLogger<EventRepository>.Instance);

        var payment = BuildPayment();
        payment.Submit();

        await repository.AppendEventsAsync(
            payment.Id,
            "Payment",
            payment.GetDomainEvents().OfType<IDomainEvent>().ToList(),
            expectedVersion: 0);

        await repository.SaveSnapshotAsync(payment.Id, payment, payment.Version);

        var succeededEvent = new PaymentSucceededEvent(
            payment.Id,
            payment.Total.Amount,
            payment.Total.Currency,
            "tx-2",
            "PSP2",
            2);

        await repository.AppendEventsAsync(
            payment.Id,
            "Payment",
            [succeededEvent],
            expectedVersion: 1);

        var loaded = await repository.GetAggregateWithSnapshotAsync<Payment>(payment.Id, Payment.RehydrateFromEvents);

        Assert.True(loaded.HasValue);
        Assert.Equal(2, loaded.Value.version);
        Assert.Equal(PaymentStatus.Processed, loaded.Value.aggregate.Status);
    }

    [Fact]
    public async Task AppendEvents_WithSameIdempotencyKey_ShouldOnlyStoreOnce()
    {
        using var connection = CreateInMemoryConnection();
        await using var db = CreateWriteDb(connection);

        var repository = new EventRepository(db, new EventSerializer(), NullLogger<EventRepository>.Instance);
        var payment = BuildPayment();
        payment.Submit();

        var idempotencyKey = Guid.NewGuid().ToString("N");
        var events = payment.GetDomainEvents().OfType<IDomainEvent>().ToList();

        await repository.AppendEventsAsync(
            payment.Id,
            "Payment",
            events,
            new EventMetadata { IdempotencyKey = idempotencyKey },
            expectedVersion: 0);

        await repository.AppendEventsAsync(
            payment.Id,
            "Payment",
            events,
            new EventMetadata { IdempotencyKey = idempotencyKey },
            expectedVersion: 1);

        var loaded = await repository.GetEventsAsync(payment.Id);
        Assert.Single(loaded);
    }

    private static SqliteConnection CreateInMemoryConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    private static WriteDbContext CreateWriteDb(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<WriteDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new WriteDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static Payment BuildPayment()
    {
        var card = Card.CreateCard("4111111111111111");
        var merchant = Merchant.CreateMerchant("Merchant A");
        var total = Money.From((100m, "USD"));
        return Payment.CreatePayment(total, card, merchant);
    }
}
