namespace PaymentRoutingPoc.Application.Handlers;

using Commands;
using Domain.Aggregates;
using Domain.Repositories;
using Domain.ValueObjects;
using DTOs;
using MediatR;
using Services;

public class CreatePaymentCommandHandler : IRequestHandler<CreatePaymentCommand, PaymentResponse>
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly ICardRepository _cardRepository;
    private readonly IMerchantRepository _merchantRepository;
    private readonly IPaymentOrchestrator _paymentOrchestrator;
    private readonly IPublisher _mediator;

    public CreatePaymentCommandHandler(
        IPaymentRepository paymentRepository,
        ICardRepository cardRepository,
        IMerchantRepository merchantRepository,
        IPaymentOrchestrator paymentOrchestrator,
        IPublisher mediator)
    {
        _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        _cardRepository = cardRepository;
        _paymentOrchestrator = paymentOrchestrator ?? throw new ArgumentNullException(nameof(paymentOrchestrator));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        _merchantRepository = merchantRepository;
    }

    public async Task<PaymentResponse> Handle(
        CreatePaymentCommand request,
        CancellationToken cancellationToken)
    {
        var money = Money.From((request.Amount, request.Currency));
        
        var card = await _cardRepository.GetByCardNumberAsync(request.CardNumber, cancellationToken);
        if (card == null)
        {
            return new PaymentResponse
            {
                Status = PaymentStatus.Failed,
                Message = "Card not found"
            };
        }
        
        var merchant = await _merchantRepository.GetByIdAsync(request.MerchantId, cancellationToken); // Assuming merchant validation is done elsewhere
        if(merchant == null)
        {
            return new PaymentResponse
            {
                Status = PaymentStatus.Failed,
                Message = "Merchant not found"
            };
        }
        
        var payment = Payment.CreatePayment(money, card, merchant);
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
            payment.MarkAsProcessed(orchestrationResult.ProviderTransactionId);
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

