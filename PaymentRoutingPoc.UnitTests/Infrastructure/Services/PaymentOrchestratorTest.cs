namespace PaymentRoutingPoc.UnitTests.Infrastructure.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using PaymentRoutingPoc.Domain.Aggregates;
using PaymentRoutingPoc.Domain.Entities;
using PaymentRoutingPoc.Domain.ValueObjects;
using PaymentRoutingPoc.Infrastructure.Psp;
using PaymentRoutingPoc.Infrastructure.Services;

public class PaymentOrchestratorTest
{
    private readonly Mock<ILogger<PaymentOrchestrator>> _logger = new();
    private readonly Mock<IPspClient> _pspClient1 = new();
    private readonly Mock<IPspClient> _pspClient2 = new();
    private readonly Payment _payment = Payment.CreatePayment(
        Money.From((100m, "USD")), 
        Card.CreateCard("4111111111111111"),
        Merchant.LoadMerchant(Guid.NewGuid(), "Test Merchant"));

    public PaymentOrchestratorTest()
    {
        _pspClient1.Setup(c => c.Priority).Returns(1);
        _pspClient2.Setup(c => c.Priority).Returns(2);
    }
    
    [Fact]
    public async Task ExecuteWithFallbackAsync_Should_Fail_When_No_PSP_Clients_Available()
    {
        // Arrange
        var services = new ServiceCollection();
        var serviceProvider = services.BuildServiceProvider();
        
        var orchestrator = new PaymentOrchestrator(serviceProvider, _logger.Object);

        // Act
        var result = await orchestrator.ExecuteWithFallbackAsync(_payment);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("No payment service providers are available", result.Message);
    }
    
    [Fact]
    public async Task ExecuteWithFallbackAsync_Should_Succeed_With_One_Client()
    {
        // Arrange
        _pspClient1.Setup(c => c.ProcessPaymentAsync(It.IsAny<PspPaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PspPaymentResponse { IsSuccess = true, TransactionId = "tx123" });
        
        var services = new ServiceCollection();
        services.AddSingleton(_pspClient1.Object);
        var serviceProvider = services.BuildServiceProvider();
        
        var orchestrator = new PaymentOrchestrator(serviceProvider, _logger.Object);

        // Act
        var result = await orchestrator.ExecuteWithFallbackAsync(_payment);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("tx123", result.ProviderTransactionId);
    }
    
    [Fact]
    public async Task ExecuteWithFallbackAsync_Should_Succeed_With_Main_Client_Success()
    {
        // Arrange
        _pspClient1.Setup(c => c.ProcessPaymentAsync(It.IsAny<PspPaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PspPaymentResponse { IsSuccess = true, TransactionId = "tx123" });
        _pspClient2.Setup(c => c.ProcessPaymentAsync(It.IsAny<PspPaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PspPaymentResponse { IsSuccess = true, TransactionId = "tx456" });
        
        var services = new ServiceCollection();
        services.AddSingleton(_pspClient1.Object);
        var serviceProvider = services.BuildServiceProvider();
        
        var orchestrator = new PaymentOrchestrator(serviceProvider, _logger.Object);

        // Act
        var result = await orchestrator.ExecuteWithFallbackAsync(_payment);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("tx123", result.ProviderTransactionId);
    }

    [Fact]
    public async Task ExecuteWithFallbackAsync_Should_Fallback_To_Secondary_Client_When_Main_Client_Fails()
    {
        // Arrange
        _pspClient1.Setup(c => c.ProcessPaymentAsync(It.IsAny<PspPaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PspPaymentResponse { IsSuccess = false, Message = "Main client failure" });
        _pspClient2.Setup(c => c.ProcessPaymentAsync(It.IsAny<PspPaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PspPaymentResponse { IsSuccess = true, TransactionId = "tx456" });

        var services = new ServiceCollection();
        services.AddSingleton(_pspClient1.Object);
        services.AddSingleton(_pspClient2.Object);
        var serviceProvider = services.BuildServiceProvider();

        var orchestrator = new PaymentOrchestrator(serviceProvider, _logger.Object);

        // Act
        var result = await orchestrator.ExecuteWithFallbackAsync(_payment);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("tx456", result.ProviderTransactionId);

        _pspClient1.Verify(c => c.ProcessPaymentAsync(It.IsAny<PspPaymentRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _logger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("PSP1 failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
    }
    
    [Fact]
    public async Task ExecuteWithFallbackAsync_Should_Fail_When_Both_Clients_Fail()
    {
        // Arrange

        _pspClient1.Setup(c => c.ProcessPaymentAsync(It.IsAny<PspPaymentRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Main client timeout"));
        
        _pspClient2.Setup(c => c.ProcessPaymentAsync(It.IsAny<PspPaymentRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PspPaymentResponse { IsSuccess = false, Message = "Secondary client failure" });

        var services = new ServiceCollection();
        services.AddSingleton(_pspClient1.Object);
        services.AddSingleton(_pspClient2.Object);
        var serviceProvider = services.BuildServiceProvider();

        var orchestrator = new PaymentOrchestrator(serviceProvider, _logger.Object);

        // Act
        var result = await orchestrator.ExecuteWithFallbackAsync(_payment);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Secondary client failure", result.Message);
    }
}