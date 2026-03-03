using Microsoft.Extensions.Logging;

namespace PaymentRoutingPoc.Infrastructure.Psp;

public class Psp2Client(HttpClient httpClient, ILogger<Psp2Client> logger) : PspClientBase(httpClient, logger)
{
    protected override string EndpointPath => "psp2";
    public override int Priority => 2;
}