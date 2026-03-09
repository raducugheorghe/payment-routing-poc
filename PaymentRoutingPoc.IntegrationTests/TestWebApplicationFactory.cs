using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace PaymentRoutingPoc.IntegrationTests;

/// <summary>
/// Integration-test host factory with explicit control over reference data seeding.
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly bool _enableReferenceDataSeed;

    public TestWebApplicationFactory(bool enableReferenceDataSeed = false)
    {
        _enableReferenceDataSeed = enableReferenceDataSeed;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ReferenceDataSeed:Enabled"] = _enableReferenceDataSeed.ToString(),
                ["ReferenceDataSeed:Environments:0"] = "Test"
            });
        });
    }
}
