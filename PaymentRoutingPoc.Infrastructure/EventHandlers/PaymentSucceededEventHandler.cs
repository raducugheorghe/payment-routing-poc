namespace PaymentRoutingPoc.Infrastructure.EventHandlers;

using Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

public class PaymentSucceededEventHandler : INotificationHandler<PaymentSucceededEvent>
{
    private readonly ILogger<PaymentSucceededEventHandler> _logger;

    public PaymentSucceededEventHandler(ILogger<PaymentSucceededEventHandler> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task Handle(PaymentSucceededEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Payment Succeeded: PaymentId={PaymentId}, Amount={Amount} {Currency}, OccurredAt={OccurredAt}",
            notification.PaymentId,
            notification.Amount,
            notification.Currency,
            notification.OccurredAt);

        _logger.LogInformation("Sending success notification for payment {PaymentId}", notification.PaymentId);

        return Task.CompletedTask;
    }
}

