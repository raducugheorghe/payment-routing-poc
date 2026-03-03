namespace PaymentRoutingPoc.Infrastructure.Psp;

using Microsoft.Extensions.Logging;

public class Psp1Client(HttpClient httpClient, ILogger<Psp1Client> logger) : PspClientBase(httpClient, logger)
{
    protected override string EndpointPath => "psp1";
    public override int Priority => 1;
}