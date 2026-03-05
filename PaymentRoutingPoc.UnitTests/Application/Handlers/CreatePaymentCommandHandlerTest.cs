using MediatR;
using Moq;
using PaymentRoutingPoc.Application.Commands;
using PaymentRoutingPoc.Application.DTOs;
using PaymentRoutingPoc.Application.Handlers;
using PaymentRoutingPoc.Application.Services;
using PaymentRoutingPoc.Domain.Aggregates;
using PaymentRoutingPoc.Domain.Entities;
using PaymentRoutingPoc.Domain.Repositories;
using PaymentRoutingPoc.Domain.ValueObjects;

namespace PaymentRoutingPoc.UnitTests.Application.Handlers;

public class CreatePaymentCommandHandlerTest
{

    private readonly CreatePaymentCommandHandler _handler;
    private readonly Mock<IPaymentRepository> _mockPaymentRepository = new();
    private readonly Mock<ICardRepository> _mockCardRepository = new();
    private readonly Mock<IPaymentOrchestrator> _mockPaymentOrchestrator = new();
    private readonly Mock<IPublisher> _mockMediator = new();
    
    public CreatePaymentCommandHandlerTest()
    {
        _handler = new CreatePaymentCommandHandler(
            _mockPaymentRepository.Object, 
            _mockCardRepository.Object,
            _mockPaymentOrchestrator.Object, 
            _mockMediator.Object);
        
            _mockCardRepository.Setup(cr => cr.GetByCardNumberAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string cardNumber, CancellationToken ct) => Card.CreateCard(cardNumber));
    }
    
    [Fact]
    public async Task Handle_ShouldReturnPaymentResponse_WhenPaymentSuccess()
    {
        // Arrange
        var command = new CreatePaymentCommand(100, "USD", "4111111111111111", "merchant123");
        
        _mockPaymentOrchestrator
            .Setup(x => x.ExecuteWithFallbackAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentOrchestratorResult{ IsSuccess = true });
        
        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PaymentStatus.Processed, result.Status);
        Assert.Equal("Payment processed successfully", result.Message);
    }

    [Fact]
    public async Task Handle_ShouldReturnPaymentResponse_WhenPaymentFails()
    {
        // Arrange
        var command = new CreatePaymentCommand(100, "USD", "411111111111", "merchant123");

        _mockPaymentOrchestrator
            .Setup(x => x.ExecuteWithFallbackAsync(It.IsAny<Payment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PaymentOrchestratorResult { IsSuccess = false, Message = "reason" });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(PaymentStatus.Failed, result.Status);
        Assert.Contains("reason", result.Message);
    }
}