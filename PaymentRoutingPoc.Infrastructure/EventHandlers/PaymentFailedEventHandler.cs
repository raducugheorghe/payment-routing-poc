namespace PaymentRoutingPoc.Infrastructure.EventHandlers;

using Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

public class PaymentFailedEventHandler : INotificationHandler<PaymentFailedEvent>
{
    private readonly ILogger<PaymentFailedEventHandler> _logger;

    public PaymentFailedEventHandler(ILogger<PaymentFailedEventHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task Handle(PaymentFailedEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Payment Failed: PaymentId={PaymentId}, Amount={Amount} {Currency}, Reason={Reason}, OccurredAt={OccurredAt}",
            notification.PaymentId,
            notification.Amount,
            notification.Currency,
            notification.Reason,
            notification.OccurredAt);

        _logger.LogWarning("Sending failure notification for payment {PaymentId}", notification.PaymentId);

        return Task.CompletedTask;
    }
}

