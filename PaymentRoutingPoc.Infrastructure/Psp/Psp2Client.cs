namespace PaymentRoutingPoc.Infrastructure.Psp;

using Microsoft.Extensions.Logging;

public class Psp2Client(IHttpClientFactory httpClientFactory, ILogger<Psp2Client> logger) 
    : PspClientBase(httpClientFactory.CreateClient(nameof(Psp2Client)), logger)
{
    protected override string EndpointPath => "psp2";
    public override int Priority => 2;
}