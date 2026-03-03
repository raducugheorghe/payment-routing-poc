namespace PaymentRoutingPoc.Infrastructure.Psp;

using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

public abstract class PspClientBase : IPspClient
{
    protected abstract string EndpointPath { get; }
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    protected PspClientBase(HttpClient httpClient, ILogger logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public abstract int Priority { get; }

    public async Task<PspPaymentResponse> ProcessPaymentAsync(
        PspPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        
        var resiliencePipeline = GetRetryResiliencePipeline(_logger);
        
        return await resiliencePipeline.ExecuteAsync(async ct =>
        {
            var httpResponse = await _httpClient.PostAsJsonAsync(
                EndpointPath,
                request,
                ct);
            
            if (httpResponse.IsSuccessStatusCode)
            {
                return await httpResponse.Content.ReadFromJsonAsync<PspPaymentResponse>(cancellationToken);            
            }

            return new PspPaymentResponse
            {
                IsSuccess = false,
                Message = httpResponse.ReasonPhrase,
            };
        }, cancellationToken);
    }
    
    private static ResiliencePipeline<PspPaymentResponse> GetRetryResiliencePipeline(ILogger logger)
    {
        // Retry strategy: 2 retries with exponential backoff
        return new ResiliencePipelineBuilder<PspPaymentResponse>()
            .AddRetry(new RetryStrategyOptions<PspPaymentResponse>
            {
                MaxRetryAttempts = 2,
                Delay = TimeSpan.FromSeconds(1),
                BackoffType = DelayBackoffType.Exponential,
                UseJitter = true,
                ShouldHandle = 
                    new PredicateBuilder<PspPaymentResponse>()
                        .HandleInner<HttpRequestException>()
                        .HandleInner<TimeoutException>(),
                OnRetry = args =>
                {
                    logger.LogWarning(
                        "Retry {RetryCount} after {DelayMs}ms due to: {Reason}",
                        args.AttemptNumber,
                        args.Duration.TotalMilliseconds,
                        args.Outcome.Exception?.Message ?? "Unknown error");
                    return default;
                }
            })
            .Build();
    }
}