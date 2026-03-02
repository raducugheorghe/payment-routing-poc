namespace PaymentRoutingPoc.Application.Handlers;

using MediatR;
using Commands;
using DTOs;
using Domain.Aggregates;
using Domain.Entities;
using Domain.Repositories;
using Domain.ValueObjects;
using Services;

public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, PaymentResponse>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentOrchestrator _paymentOrchestrator;
    private readonly IPublisher _mediator;

    public CreatePaymentCommandHandler(
        IPaymentRepository paymentRepository,
        IPaymentOrchestrator paymentOrchestrator,
        IPublisher mediator)
    {
        _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        _paymentOrchestrator = paymentOrchestrator ?? throw new ArgumentNullException(nameof(paymentOrchestrator));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    public async Task<PaymentResponse> Handle(
        CreatePaymentCommand request,
        CancellationToken cancellationToken)
    {
        var money = Money.From((request.Amount, request.Currency));
        
        //TODO: load card details from repository instead of creating a new one with just the number
        var card = Card.CreateCard(request.CardNumber);
        
        var payment = Payment.CreatePayment(money, card);
        payment.Submit();

        // Save initial state
        await _paymentRepository.SaveAsync(payment, cancellationToken);

        // Publish submitted events
        foreach (var domainEvent in payment.GetDomainEvents())
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
        payment.ClearDomainEvents();

        // Execute payment with fallback
        var orchestrationResult = await _paymentOrchestrator.ExecuteWithFallbackAsync(payment, cancellationToken);
        
        if (orchestrationResult.IsSuccess)
        {
            payment.MarkAsProcessed();
        }
        else
        {
            payment.MarkAsFailed(orchestrationResult.Message);
        }

        // Save final state
        await _paymentRepository.SaveAsync(payment, cancellationToken);

        // Publish final events
        foreach (var domainEvent in payment.GetDomainEvents())
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
        payment.ClearDomainEvents();

        return new PaymentResponse
        {
            PaymentId = payment.Id,
            Status = payment.Status,
            Message = orchestrationResult.IsSuccess
                ? "Payment processed successfully"
                : $"Payment failed: {orchestrationResult.Message}"
        };
    }
}

