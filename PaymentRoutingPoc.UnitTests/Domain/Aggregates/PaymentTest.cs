using PaymentRoutingPoc.Domain.Aggregates;
using PaymentRoutingPoc.Domain.Entities;
using PaymentRoutingPoc.Domain.Events;
using PaymentRoutingPoc.Domain.ValueObjects;

namespace PaymentRoutingPoc.UnitTests.Domain.Aggregates;

public class PaymentTest
{
    private readonly Card _card = Card.CreateCard("test pan");
    private readonly Money _total = Money.From((100.00m, "USD"));
    
    [Fact]
    public void CreatePayment_ShouldInitializeProperties()
    {
        // Arrange & Act
        var payment = Payment.CreatePayment(_total, _card);

        // Assert
        Assert.NotNull(payment);
        Assert.Equal(_card.Id, payment.Card.Id);
        Assert.Equal(_total, payment.Total);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
    }

    [Fact]
    public void Submit_ShouldAddPaymentSubmittedEvent()
    {
        // Arrange
        var payment = Payment.CreatePayment(_total, _card);

        // Act
        payment.Submit();

        // Assert
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Contains(payment.GetDomainEvents(),
            e => e is PaymentSubmittedEvent submittedEvent &&
                 submittedEvent.PaymentId == payment.Id &&
                 submittedEvent.Amount == _total.Amount &&
                 submittedEvent.Currency == _total.Currency);
    }

    [Fact]
    public void MarkAsProcessed_ShouldUpdateStatusAndAddPaymentSucceededEvent()
    {
        // Arrange
        var payment = Payment.CreatePayment(_total, _card);
        payment.Submit();

        // Act
        payment.MarkAsProcessed("provider-tx-123");

        // Assert
        Assert.Equal(PaymentStatus.Processed, payment.Status);
        Assert.Contains(payment.GetDomainEvents(),
            e => e is PaymentSucceededEvent succeededEvent &&
                 succeededEvent.PaymentId == payment.Id &&
                 succeededEvent.Amount == _total.Amount &&
                 succeededEvent.Currency == _total.Currency);
    }

    [Fact]
    public void MarkAsFailed_ShouldUpdateStatusAndAddPaymentFailedEvent()
    {
        // Arrange
        var payment = Payment.CreatePayment(_total, _card);
        payment.Submit();

        // Act
        var reason = "Insufficient funds";
        payment.MarkAsFailed(reason);

        // Assert
        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Contains(payment.GetDomainEvents(),
            e => e is PaymentFailedEvent failedEvent &&
                 failedEvent.PaymentId == payment.Id &&
                 failedEvent.Amount == _total.Amount &&
                 failedEvent.Currency == _total.Currency &&
                 failedEvent.Reason == reason);
    }
}