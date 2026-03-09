using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PaymentRoutingPoc.Persistence.DbContexts;
using PaymentRoutingPoc.Persistence.Models.Read;

namespace PaymentRoutingPoc.Persistence.Configuration;

/// <summary>
/// Seeds local-only reference data for read-model card and merchant records.
/// </summary>
public class ReferenceDataSeeder : IReferenceDataSeeder
{
    private readonly ReadDbContext _readDb;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<ReferenceDataSeeder> _logger;

    public ReferenceDataSeeder(
        ReadDbContext readDb,
        IConfiguration configuration,
        IHostEnvironment environment,
        ILogger<ReferenceDataSeeder> logger)
    {
        _readDb = readDb ?? throw new ArgumentNullException(nameof(readDb));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _environment = environment ?? throw new ArgumentNullException(nameof(environment));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var options = LoadOptions(_configuration);
        if (!options.Enabled)
        {
            return;
        }

        if (options.Environments.Length > 0 &&
            !options.Environments.Contains(_environment.EnvironmentName, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        var cardsAdded = 0;
        var merchantsAdded = 0;

        foreach (var seedCard in options.Cards)
        {
            if (string.IsNullOrWhiteSpace(seedCard.CardId) || string.IsNullOrWhiteSpace(seedCard.CardNumber))
            {
                continue;
            }

            var exists = await _readDb.Cards.AnyAsync(
                c => c.CardId == seedCard.CardId || c.CardNumber == seedCard.CardNumber,
                cancellationToken);

            if (exists)
            {
                continue;
            }

            _readDb.Cards.Add(new CardRecord
            {
                CardId = seedCard.CardId,
                CardNumber = seedCard.CardNumber,
                CreatedAt = DateTime.UtcNow
            });

            cardsAdded++;
        }

        foreach (var seedMerchant in options.Merchants)
        {
            if (string.IsNullOrWhiteSpace(seedMerchant.MerchantId) || string.IsNullOrWhiteSpace(seedMerchant.Name))
            {
                continue;
            }

            var exists = await _readDb.Merchants.AnyAsync(
                m => m.MerchantId == seedMerchant.MerchantId,
                cancellationToken);

            if (exists)
            {
                continue;
            }

            _readDb.Merchants.Add(new MerchantRecord
            {
                MerchantId = seedMerchant.MerchantId,
                Name = seedMerchant.Name,
                CreatedAt = DateTime.UtcNow
            });

            merchantsAdded++;
        }

        if (cardsAdded > 0 || merchantsAdded > 0)
        {
            await _readDb.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Seeded reference data in {Environment}. Cards added: {CardsAdded}, Merchants added: {MerchantsAdded}",
                _environment.EnvironmentName,
                cardsAdded,
                merchantsAdded);
        }
    }

    private static ReferenceDataSeedOptions LoadOptions(IConfiguration configuration)
    {
        var section = configuration.GetSection("ReferenceDataSeed");
        var options = new ReferenceDataSeedOptions
        {
            Enabled = bool.TryParse(section["Enabled"], out var enabled) && enabled,
            Environments = section.GetSection("Environments").GetChildren()
                .Select(c => c.Value)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!)
                .ToArray(),
            Cards = section.GetSection("Cards").GetChildren()
                .Select(c => new SeedCard
                {
                    CardId = c["CardId"] ?? string.Empty,
                    CardNumber = c["CardNumber"] ?? string.Empty
                })
                .ToList(),
            Merchants = section.GetSection("Merchants").GetChildren()
                .Select(c => new SeedMerchant
                {
                    MerchantId = c["MerchantId"] ?? string.Empty,
                    Name = c["Name"] ?? string.Empty
                })
                .ToList()
        };

        return options;
    }
}
