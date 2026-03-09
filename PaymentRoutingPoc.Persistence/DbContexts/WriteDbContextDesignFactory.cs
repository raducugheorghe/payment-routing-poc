using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PaymentRoutingPoc.Persistence.DbContexts;

/// <summary>
/// Design-time factory for WriteDbContext.
/// Enables Entity Framework CLI tooling to work with WriteDbContext during development.
/// </summary>
public class WriteDbContextDesignFactory : IDesignTimeDbContextFactory<WriteDbContext>
{
    public WriteDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WriteDbContext>();
        
        // Use a development database path
        var dbPath = Path.Combine(
            AppContext.BaseDirectory,
            "payment-write-dev.db"
        );
        
        optionsBuilder.UseSqlite($"Data Source={dbPath}");
        
        return new WriteDbContext(optionsBuilder.Options);
    }
}
