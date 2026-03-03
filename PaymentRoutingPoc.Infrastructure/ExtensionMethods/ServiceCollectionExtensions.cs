namespace PaymentRoutingPoc.Infrastructure.ExtensionMethods;

using Microsoft.Extensions.DependencyInjection;
using PaymentRoutingPoc.Application.Services;
using PaymentRoutingPoc.Domain.Repositories;
using Psp;
using Repositories;
using Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddHttpClient<Psp1Client>(c=>
        {
            c.BaseAddress = new Uri("http://localhost:5156/");
            c.Timeout = TimeSpan.FromSeconds(3);
        });
        
        services.AddHttpClient<Psp2Client>(c=>
        {
            c.BaseAddress = new Uri("http://localhost:5156/");
            c.Timeout = TimeSpan.FromSeconds(3);
        });
        
        services.AddTransient<IPspClient, Psp1Client>();
        services.AddTransient<IPspClient, Psp2Client>();
        
        services.AddSingleton<IPaymentRepository, InMemoryPaymentRepository>();

        services.AddTransient<IPaymentOrchestrator, PaymentOrchestrator>();
        
        return services;
    }
}