namespace PaymentRoutingPoc.Application.Commands;

using DTOs;
using MediatR;

public class CreatePaymentCommand : IRequest<PaymentResponse>
{
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public string CardNumber { get; set; }
    public string MerchantId { get; set; }

    public CreatePaymentCommand(
        decimal amount,
        string currency,
        string cardNumber,
        string merchantId)
    {
        Amount = amount;
        Currency = currency;
        CardNumber = cardNumber;
        MerchantId = merchantId;
    }
}

