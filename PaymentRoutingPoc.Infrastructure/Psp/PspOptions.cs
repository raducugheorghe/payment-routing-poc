namespace PaymentRoutingPoc.Infrastructure.Psp;

public class PspOptions<T> where T : IPspClient
{
    public string BaseUrl { get; set; }
    public int TimeoutInSeconds { get; set; }
}