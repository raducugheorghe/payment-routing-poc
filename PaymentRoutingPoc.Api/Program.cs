var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapPost("/psp1", async (PaymentRequest request) =>
        {
            // Simulate processing the payment request with PSP1
            var shouldFail = Random.Shared.Next(2) == 0;
            
            if (shouldFail)
            {
                // simulate processing time with a random delay between 1 and 30s
                var delay = Random.Shared.Next(1000, 30000);
                await Task.Delay(delay);
                
                return Results.BadRequest(new PaymentResponse
                {
                    Success = false,
                    Message = "Payment processing failed with PSP1"
                });
            }
            
            var response = new PaymentResponse
            {
                Success = true,
                Message = "Payment processed successfully with PSP1"
        };
            
        return Results.Ok(response);
    })
    .WithName("ProcessPaymentWithPSP1");

app.MapPost("/psp2", (PaymentRequest request) =>
    {
        // Simulate processing the payment request with PSP2
        var response = new PaymentResponse
        {
            Success = true,
            Message = "Payment processed successfully with PSP2"
        };
        return Results.Ok(response);
    })
    .WithName("ProcessPaymentWithPSP2");

app.MapPost("/api/payments", (PaymentRequest request) =>
{
    
});

app.Run();


public class PaymentRequest
{
    public required decimal Amount { get; set; }
    public required string Currency { get; set; }
    public required string CardNumber { get; set; }
    public Guid MerchantId { get; set; } = Guid.NewGuid();
}

public class PaymentResponse
{
    public bool Success { get; set; }
    public string Message { get; set; }
}