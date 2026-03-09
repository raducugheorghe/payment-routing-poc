using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using PaymentRoutingPoc.Domain.Events;
using PaymentRoutingPoc.Persistence.DbContexts;
using PaymentRoutingPoc.Persistence.Models.Read;

namespace PaymentRoutingPoc.Persistence.Projections;

/// <summary>
/// Projects payment events into the PaymentReadModel for fast queries.
/// Handles PaymentSubmittedEvent, PaymentSucceededEvent, and PaymentFailedEvent.
/// </summary>
public class PaymentProjection : IProjection
{
    public string ProjectionId => nameof(PaymentProjection);

    private readonly ReadDbContext _readDb;
    private readonly ILogger<PaymentProjection> _logger;

    public PaymentProjection(ReadDbContext readDb, ILogger<PaymentProjection> logger)
    {
        _readDb = readDb ?? throw new ArgumentNullException(nameof(readDb));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task HandleAsync(IDomainEvent domainEvent, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        try
        {
            switch (domainEvent)
            {
                case PaymentSubmittedEvent submitted:
                    await HandlePaymentSubmittedAsync(submitted, cancellationToken);
                    break;

                case PaymentSucceededEvent succeeded:
                    await HandlePaymentSucceededAsync(succeeded, cancellationToken);
                    break;

                case PaymentFailedEvent failed:
                    await HandlePaymentFailedAsync(failed, cancellationToken);
                    break;

                default:
                    _logger.LogWarning(
                        "PaymentProjection received unhandled event type: {EventType}",
                        domainEvent.GetType().Name);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error handling event {EventType} in PaymentProjection for payment {PaymentId}",
                domainEvent.GetType().Name,
                domainEvent.AggregateId);
            throw;
        }
    }

    private async Task HandlePaymentSubmittedAsync(
        PaymentSubmittedEvent @event,
        CancellationToken cancellationToken)
    {
        var paymentId = @event.PaymentId.ToString();

        // Check if payment already exists (idempotency)
        var existing = await _readDb.PaymentsReadModel
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId, cancellationToken);

        if (existing != null)
        {
            _logger.LogInformation(
                "PaymentReadModel already exists for payment {PaymentId}. Skipping duplicate.",
                paymentId);
            return;
        }

        var readModel = new PaymentReadModel
        {
            PaymentId = paymentId,
            Amount = @event.Amount,
            Currency = @event.Currency,
            Status = "Pending",
            CardId = @event.CardId.ToString(),
            CardNumber = MaskCardFromLast4(@event.CardLast4),
            MerchantId = @event.MerchantId.ToString(),
            MerchantName = @event.MerchantName,
            CreatedAt = @event.OccurredAt,
            LastEventType = nameof(PaymentSubmittedEvent),
            AggregateVersion = @event.AggregateVersion
        };

        _readDb.PaymentsReadModel.Add(readModel);

        // Add event log entry for audit
        await AddEventLogAsync(paymentId, @event, cancellationToken);
    }

    private async Task HandlePaymentSucceededAsync(
        PaymentSucceededEvent @event,
        CancellationToken cancellationToken)
    {
        var paymentId = @event.PaymentId.ToString();

        var readModel = await GetPaymentReadModelAsync(paymentId, cancellationToken);

        if (readModel == null)
        {
            _logger.LogWarning(
                "PaymentReadModel not found for payment {PaymentId} in PaymentSucceededEvent. Skipping.",
                paymentId);
            return;
        }

        readModel.Status = "Processed";
        readModel.ProcessedAt = @event.OccurredAt;
        readModel.ProviderTransactionId = @event.ProviderTransactionId;
        readModel.ProviderName = @event.ProviderName;
        readModel.LastEventType = nameof(PaymentSucceededEvent);
        readModel.AggregateVersion = @event.AggregateVersion;

        _readDb.PaymentsReadModel.Update(readModel);

        // Add event log entry for audit
        await AddEventLogAsync(paymentId, @event, cancellationToken);
    }

    private async Task HandlePaymentFailedAsync(
        PaymentFailedEvent @event,
        CancellationToken cancellationToken)
    {
        var paymentId = @event.PaymentId.ToString();

        var readModel = await GetPaymentReadModelAsync(paymentId, cancellationToken);

        if (readModel == null)
        {
            _logger.LogWarning(
                "PaymentReadModel not found for payment {PaymentId} in PaymentFailedEvent. Skipping.",
                paymentId);
            return;
        }

        readModel.Status = "Failed";
        readModel.FailureReason = @event.Reason;
        readModel.LastEventType = nameof(PaymentFailedEvent);
        readModel.AggregateVersion = @event.AggregateVersion;

        _readDb.PaymentsReadModel.Update(readModel);

        // Add event log entry for audit
        await AddEventLogAsync(paymentId, @event, cancellationToken);
    }

    private async Task AddEventLogAsync(
        string paymentId,
        IDomainEvent @event,
        CancellationToken cancellationToken)
    {
        var eventLogId = BuildDeterministicEventLogId(paymentId, @event);

        if (_readDb.PaymentEventLogs.Local.Any(e => e.EventLogId == eventLogId))
            return;

        var existing = await _readDb.PaymentEventLogs
            .AsNoTracking()
            .AnyAsync(e => e.EventLogId == eventLogId, cancellationToken);

        if (existing)
            return;

        var eventLog = new PaymentEventLog
        {
            EventLogId = eventLogId,
            PaymentId = paymentId,
            EventType = @event.GetType().Name,
            EventData = System.Text.Json.JsonSerializer.Serialize(@event),
            OccurredAt = @event.OccurredAt
        };

        _readDb.PaymentEventLogs.Add(eventLog);
    }

    private async Task<PaymentReadModel?> GetPaymentReadModelAsync(
        string paymentId,
        CancellationToken cancellationToken)
    {
        var local = _readDb.PaymentsReadModel.Local.FirstOrDefault(p => p.PaymentId == paymentId);
        if (local != null)
            return local;

        return await _readDb.PaymentsReadModel
            .FirstOrDefaultAsync(p => p.PaymentId == paymentId, cancellationToken);
    }

    private static string BuildDeterministicEventLogId(string paymentId, IDomainEvent @event)
    {
        var raw = $"{paymentId}:{@event.GetType().Name}:{@event.AggregateVersion}:{@event.OccurredAt:O}";
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(raw));
        return new Guid(bytes).ToString();
    }

    private static string MaskCardFromLast4(string last4)
    {
        if (string.IsNullOrWhiteSpace(last4) || last4.Length != 4)
            return "**** **** **** ****";

        return $"**** **** **** {last4}";
    }
}
