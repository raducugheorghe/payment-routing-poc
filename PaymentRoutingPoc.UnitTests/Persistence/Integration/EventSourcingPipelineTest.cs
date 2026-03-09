using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PaymentRoutingPoc.Domain.Aggregates;
using PaymentRoutingPoc.Domain.Entities;
using PaymentRoutingPoc.Domain.ValueObjects;
using PaymentRoutingPoc.Infrastructure.Repositories;
using PaymentRoutingPoc.Persistence.DbContexts;
using PaymentRoutingPoc.Persistence.Projections;
using PaymentRoutingPoc.Persistence.Repositories.EventStore;
using PaymentRoutingPoc.Persistence.Serialization;

namespace PaymentRoutingPoc.UnitTests.Persistence.Integration;

public class EventSourcingPipelineTest
{
    [Fact]
    public async Task SaveEvents_ThenProjectTwice_ShouldRemainIdempotent()
    {
        await using var writeConnection = new SqliteConnection("Data Source=:memory:");
        await using var readConnection = new SqliteConnection("Data Source=:memory:");
        await writeConnection.OpenAsync();
        await readConnection.OpenAsync();

        await using var writeDb = CreateWriteDb(writeConnection);
        await using var readDb = CreateReadDb(readConnection);

        var eventRepository = new EventRepository(writeDb, new EventSerializer(), NullLogger<EventRepository>.Instance);
        var paymentRepository = new EventSourcedPaymentRepository(eventRepository);

        var paymentProjection = new PaymentProjection(readDb, NullLogger<PaymentProjection>.Instance);
        var merchantProjection = new MerchantStatisticsProjection(readDb, NullLogger<MerchantStatisticsProjection>.Instance);
        var processor = new ProjectionProcessor(
            writeDb,
            readDb,
            new EventSerializer(),
            NullLogger<ProjectionProcessor>.Instance);

        var payment = BuildPayment();
        payment.Submit();
        await paymentRepository.SaveAsync(payment);

        payment.MarkAsProcessed("tx-123", "PSP1");
        await paymentRepository.SaveAsync(payment);

        await processor.ProcessPendingEventsAsync([paymentProjection, merchantProjection]);
        await processor.ProcessPendingEventsAsync([paymentProjection, merchantProjection]);

        var readModel = await readDb.PaymentsReadModel.SingleAsync();
        var eventLogs = await readDb.PaymentEventLogs.ToListAsync();
        var stats = await readDb.MerchantPaymentStatistics.ToListAsync();

        Assert.Equal("Processed", readModel.Status);
        Assert.Equal(payment.Card.Id.ToString(), readModel.CardId);
        Assert.Equal(payment.Merchant.Id.ToString(), readModel.MerchantId);
        Assert.Equal("tx-123", readModel.ProviderTransactionId);

        Assert.Equal(2, eventLogs.Count);
        Assert.True(stats.Count <= 1);
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

    private static ReadDbContext CreateReadDb(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<ReadDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new ReadDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static Payment BuildPayment()
    {
        var card = Card.CreateCard("4111111111111111");
        var merchant = Merchant.CreateMerchant("Merchant C");
        var total = Money.From((42m, "USD"));
        return Payment.CreatePayment(total, card, merchant);
    }
}
