namespace PaymentRoutingPoc.Infrastructure.Services;

using Application.DTOs;
using Application.Services;
using Domain.Aggregates;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Fallback;
using Psp;

public class PaymentOrchestrator : IPaymentOrchestrator
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentOrchestrator> _logger;

    public PaymentOrchestrator(IServiceProvider serviceProvider,
        ILogger<PaymentOrchestrator> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PaymentOrchestratorResult> ExecuteWithFallbackAsync(
        Payment payment,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting payment processing: {PaymentId}", payment.Id);
            
            // Get first 2 available PSP clients ordered by priority
            var pspClients = _serviceProvider.GetServices<IPspClient>()
                .OrderBy(psp => psp.Priority)
                .Take(2)
                .ToList();

            if (pspClients.Count == 0)
            {
                _logger.LogError("No PSP clients were found");
                return new PaymentOrchestratorResult
                {
                    IsSuccess = false,
                    Message = "No payment service providers are available"
                };
            }
            
            var mainClient = pspClients[0];
            var fallbackClient = pspClients.Count > 1 ? pspClients[1] : null;
            
            var pspRequest = new PspPaymentRequest
            {
                Amount =  payment.Total.Amount,
                Currency = payment.Total.Currency,
                CardNumber = payment.Card.CardNumber
            };
            
            PspPaymentResponse pspPaymentResponse;
            if (fallbackClient is null)
            {
                pspPaymentResponse = await mainClient.ProcessPaymentAsync(pspRequest, cancellationToken);
            }
            else
            {
                var resiliencePolicy = GetFallbackResiliencePipeline(
                    fallbackClient,
                    pspRequest,
                    _logger);

                pspPaymentResponse = await resiliencePolicy.ExecuteAsync(
                    async (context, ct) => await mainClient.ProcessPaymentAsync(pspRequest, ct),
                    cancellationToken);
            }
            

            if (pspPaymentResponse.IsSuccess)
            {
                _logger.LogInformation("Payment successful: {TransactionId}. {Message}", pspPaymentResponse.TransactionId, pspPaymentResponse.Message);
                return new PaymentOrchestratorResult
                {
                    IsSuccess = true,
                    Message = "Payment processed successfully via " + (pspPaymentResponse.Message ?? "Unknown"),
                    ProviderTransactionId =  pspPaymentResponse.TransactionId,
                    ProviderName = pspPaymentResponse.Message ?? "Unknown"
                };
            }

            _logger.LogWarning("Payment failed: {Message}", pspPaymentResponse.Message);
            return new PaymentOrchestratorResult
            {
                IsSuccess = false,
                Message = pspPaymentResponse.Message ?? "Unknown error"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment orchestration error: {Message}", ex.Message);
            return new PaymentOrchestratorResult
            {
                IsSuccess = false,
                Message = "Payment processing failed: " + ex.Message
            };
        }
    }
    
    public static ResiliencePipeline<PspPaymentResponse> GetFallbackResiliencePipeline(
        IPspClient fallbackClient,
        PspPaymentRequest fallbackRequest,
        ILogger logger)
    {
        return  new ResiliencePipelineBuilder<PspPaymentResponse>()
            .AddFallback(new FallbackStrategyOptions<PspPaymentResponse>
            {
                ShouldHandle = new PredicateBuilder<PspPaymentResponse>()
                    .HandleInner<TimeoutException>()
                    .HandleInner<HttpRequestException>()
                    .HandleResult(r => !r.IsSuccess),
                FallbackAction = async (context) =>
                {
                    logger.LogInformation("PSP1 failed, falling back to PSP2...");
                    var result = await fallbackClient.ProcessPaymentAsync(fallbackRequest, context.Context.CancellationToken);
                    logger.LogInformation("Fallback invoked: {Message}", result?.Message ?? "Unknown");
                    return Outcome.FromResult(result);
                }
            })
            .Build();
    }
}



