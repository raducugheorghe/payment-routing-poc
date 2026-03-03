namespace PaymentRoutingPoc.Infrastructure.Psp;

using Microsoft.Extensions.Logging;

public class Psp1Client : PspClientBase
{
    public Psp1Client(IHttpClientFactory httpClientFactory, ILogger<Psp1Client> logger)
        : base(httpClientFactory.CreateClient(nameof(Psp1Client)), logger)
    {
    }

    protected override string EndpointPath => "psp1";
    public override int Priority => 1;
}