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

    public CreatePaymentCommandHandler(
        IPaymentRepository paymentRepository,
        ICardRepository cardRepository,
        IMerchantRepository merchantRepository,
        IPaymentOrchestrator paymentOrchestrator)
    {
        _paymentRepository = paymentRepository ?? throw new ArgumentNullException(nameof(paymentRepository));
        _cardRepository = cardRepository;
        _paymentOrchestrator = paymentOrchestrator ?? throw new ArgumentNullException(nameof(paymentOrchestrator));
        _merchantRepository = merchantRepository;
    }

    public async Task<PaymentResponse> Handle(
        CreatePaymentCommand request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.IdempotencyKey))
        {
            var existingPayment = await _paymentRepository.GetByIdempotencyKeyAsync(request.IdempotencyKey, cancellationToken);
            if (existingPayment != null)
            {
                return new PaymentResponse
                {
                    PaymentId = existingPayment.Id,
                    Status = existingPayment.Status,
                    Message = "Payment request already processed"
                };
            }
        }

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

        // Persist create transition with optional idempotency key.
        await _paymentRepository.SaveAsync(payment, request.IdempotencyKey, cancellationToken);

        // Execute payment with fallback
        var orchestrationResult = await _paymentOrchestrator.ExecuteWithFallbackAsync(payment, cancellationToken);
        
        if (orchestrationResult.IsSuccess)
        {
            payment.MarkAsProcessed(orchestrationResult.ProviderTransactionId, orchestrationResult.ProviderName);
        }
        else
        {
            payment.MarkAsFailed(orchestrationResult.Message);
        }

        await _paymentRepository.SaveAsync(payment, cancellationToken: cancellationToken);

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

