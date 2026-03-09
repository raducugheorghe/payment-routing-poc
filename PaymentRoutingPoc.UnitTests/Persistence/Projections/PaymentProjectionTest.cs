using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using PaymentRoutingPoc.Domain.Events;
using PaymentRoutingPoc.Persistence.DbContexts;
using PaymentRoutingPoc.Persistence.Projections;

namespace PaymentRoutingPoc.UnitTests.Persistence.Projections;

public class PaymentProjectionTest
{
    [Fact]
    public async Task HandleSubmittedEvent_ShouldPersistDenormalizedValues()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ReadDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var readDb = new ReadDbContext(options);
        await readDb.Database.EnsureCreatedAsync();

        var projection = new PaymentProjection(readDb, NullLogger<PaymentProjection>.Instance);

        var submitted = new PaymentSubmittedEvent(
            Guid.NewGuid(),
            75m,
            "EUR",
            Guid.NewGuid(),
            "4444",
            Guid.NewGuid(),
            "Merchant B",
            1);

        await projection.HandleAsync(submitted, CancellationToken.None);
        await readDb.SaveChangesAsync();

        var model = await readDb.PaymentsReadModel.SingleAsync();

        Assert.Equal("Pending", model.Status);
        Assert.Equal(submitted.CardId.ToString(), model.CardId);
        Assert.Equal("**** **** **** 4444", model.CardNumber);
        Assert.Equal(submitted.MerchantId.ToString(), model.MerchantId);
        Assert.Equal(submitted.MerchantName, model.MerchantName);
        Assert.Equal(submitted.AggregateVersion, model.AggregateVersion);
    }
}
