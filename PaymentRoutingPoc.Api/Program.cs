using PaymentRoutingPoc.Application.Commands;
using PaymentRoutingPoc.Application.DTOs;
using PaymentRoutingPoc.Infrastructure.EventHandlers;
using PaymentRoutingPoc.Infrastructure.ExtensionMethods;
using PaymentRoutingPoc.Infrastructure.Psp;
using MediatR;


var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddOpenApi();
builder.Services.AddLogging();

// Register MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining(typeof(CreatePaymentCommand));
    cfg.RegisterServicesFromAssemblyContaining(typeof(PaymentSucceededEventHandler));
});

builder.Services.AddInfrastructure(builder.Configuration);



var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/psp1", async (PspPaymentRequest request) =>
        {
            // Simulate processing the payment request with PSP1, fail rate of 50%
            var shouldFail = Random.Shared.Next(2) == 0;

            if (shouldFail)
            {
                // simulate processing time with a random delay between 1 and 6s
                var delay = Random.Shared.Next(1000, 6000);
                await Task.Delay(delay);

                return Results.BadRequest(new PspPaymentResponse
                {
                    IsSuccess = false,
                    Message = "Payment processing failed with PSP1"
                });
            }

            var response = new PspPaymentResponse
            {
                IsSuccess = true,
                TransactionId = Guid.NewGuid().ToString(),
                Message = "Payment processed successfully with PSP1"
        };

        return Results.Ok(response);
    })
    .WithName("ProcessPaymentWithPSP1");

app.MapPost("/psp2", (PspPaymentRequest request) =>
    {
        // Simulate processing the payment request with PSP2, fail rate of 50%
        var shouldFail = Random.Shared.Next(2) == 0;

        if (shouldFail)
        {
            return Results.BadRequest(new PspPaymentResponse
            {
                IsSuccess = false,
                Message = "Payment processing failed with PSP2"
            });
        }

        var response = new PspPaymentResponse
        {
            IsSuccess = true,
            TransactionId = Guid.NewGuid().ToString(),
            Message = "Payment processed successfully with PSP2"
        };

        return Results.Ok(response);
    })
    .WithName("ProcessPaymentWithPSP2");

app.MapPost("/api/payments", async (CreatePaymentRequest request, IMediator mediator) =>
    {
        if(!Guid.TryParse(request.MerchantId, out var merchantId))
        {
            return Results.BadRequest("Invalid MerchantId");
        }
        
        
        var command = new CreatePaymentCommand(
            request.Amount,
            request.Currency,
            request.CardNumber,
            merchantId);
        
        var result = await mediator.Send(command);
        
        return Results.Ok(result);
    })
    .WithName("ProcessPayment");

app.Run();

