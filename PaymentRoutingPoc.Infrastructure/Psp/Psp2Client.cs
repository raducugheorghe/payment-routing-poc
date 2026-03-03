namespace PaymentRoutingPoc.Infrastructure.Psp;

using Microsoft.Extensions.Logging;

public class Psp2Client(HttpClient httpClient, ILogger<Psp2Client> logger) : PspClientBase(httpClient, logger)
{
    protected override string EndpointPath => "psp2";
    public override int Priority => 2;
}