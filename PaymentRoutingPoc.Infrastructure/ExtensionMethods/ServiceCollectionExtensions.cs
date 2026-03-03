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
            c.BaseAddress = new Uri("https://localhost:5001/");
        });
        
        services.AddHttpClient<Psp2Client>(c=>
        {
            c.BaseAddress = new Uri("https://localhost:5001/");
        });

        services.AddSingleton<IPaymentRepository, InMemoryPaymentRepository>();

        services.AddScoped<IPaymentOrchestrator, PaymentOrchestrator>();
        
        return services;
    }
}