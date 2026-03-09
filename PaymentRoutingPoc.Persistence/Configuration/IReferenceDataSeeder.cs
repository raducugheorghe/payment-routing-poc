namespace PaymentRoutingPoc.Persistence.Configuration;

public interface IReferenceDataSeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}
