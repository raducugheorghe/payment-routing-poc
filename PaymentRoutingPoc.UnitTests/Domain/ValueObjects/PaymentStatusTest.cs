using PaymentRoutingPoc.Domain.ValueObjects;

namespace PaymentRoutingPoc.UnitTests.Domain.ValueObjects;

public class PaymentStatusTest
{
    [Fact]
    public void Pending_CanTransitionTo_ProcessedOrFailed()
    {
        // Arrange
        var pending = PaymentStatus.Pending;

        // Act & Assert
        Assert.True(pending.CanTransitionTo(PaymentStatus.Processed));
        Assert.True(pending.CanTransitionTo(PaymentStatus.Failed));
    }

    [Fact]
    public void Processed_CannotTransitionToAnyOtherState()
    {
        // Arrange
        var processed = PaymentStatus.Processed;

        // Act & Assert
        Assert.False(processed.CanTransitionTo(PaymentStatus.Pending));
        Assert.False(processed.CanTransitionTo(PaymentStatus.Failed));
    }

    [Fact]
    public void Failed_CannotTransitionToAnyOtherState()
    {
        // Arrange
        var failed = PaymentStatus.Failed;

        // Act & Assert
        Assert.False(failed.CanTransitionTo(PaymentStatus.Pending));
        Assert.False(failed.CanTransitionTo(PaymentStatus.Processed));
    }

    [Fact]
    public void TransitionTo_ShouldThrow_WhenTransitionIsInvalid()
    {
        // Arrange
        var processed = PaymentStatus.Processed;

        // Act
        var action = () => processed.TransitionTo(PaymentStatus.Failed);

        // Assert
        var exception = Assert.Throws<InvalidOperationException>(action);
        Assert.Contains("Invalid payment status transition", exception.Message);
    }
}
