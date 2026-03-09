using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PaymentRoutingPoc.Persistence.DbContexts;

/// <summary>
/// Design-time factory for ReadDbContext.
/// Enables Entity Framework CLI tooling to work with ReadDbContext during development.
/// </summary>
public class ReadDbContextDesignFactory : IDesignTimeDbContextFactory<ReadDbContext>
{
    public ReadDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ReadDbContext>();
        
        // Use a development database path
        var dbPath = Path.Combine(
            AppContext.BaseDirectory,
            "payment-read-dev.db"
        );
        
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        
        return new ReadDbContext(optionsBuilder.Options);
    }
}
