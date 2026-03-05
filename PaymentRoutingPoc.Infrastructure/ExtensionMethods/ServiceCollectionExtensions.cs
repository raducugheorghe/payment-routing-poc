using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace PaymentRoutingPoc.Infrastructure.ExtensionMethods;

using Microsoft.Extensions.DependencyInjection;
using PaymentRoutingPoc.Application.Services;
using PaymentRoutingPoc.Domain.Repositories;
using Psp;
using Repositories;
using Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PspOptions<Psp1Client>>(configuration.GetSection($"{nameof(PspOptions<>)}:{nameof(Psp1Client)}"));
        services.Configure<PspOptions<Psp2Client>>(configuration.GetSection($"{nameof(PspOptions<>)}:{nameof(Psp2Client)}"));
        
        services.AddHttpClient<Psp1Client>((sp, c)=>
        {
            var options = sp.GetRequiredService<IOptions<PspOptions<Psp1Client>>>().Value;
            c.BaseAddress = new Uri(options.BaseUrl);
            c.Timeout = TimeSpan.FromSeconds(options.TimeoutInSeconds);
        });
        
        services.AddHttpClient<Psp2Client>((sp, c)=>
        {
            var options = sp.GetRequiredService<IOptions<PspOptions<Psp2Client>>>().Value;
            c.BaseAddress = new Uri(options.BaseUrl);
            c.Timeout = TimeSpan.FromSeconds(options.TimeoutInSeconds);
        });
        
        services.AddTransient<IPspClient, Psp1Client>();
        services.AddTransient<IPspClient, Psp2Client>();
        
        services.AddSingleton<IPaymentRepository, InMemoryPaymentRepository>();
        services.AddSingleton<ICardRepository, InMemoryCardRepository>();
        services.AddSingleton<IMerchantRepository, InMemoryMerchantRepository>();

        services.AddTransient<IPaymentOrchestrator, PaymentOrchestrator>();
        
        return services;
    }
}