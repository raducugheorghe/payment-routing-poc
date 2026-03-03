namespace PaymentRoutingPoc.Infrastructure.Psp;

public interface IPspClient
{
    int Priority { get; }
    Task<PspPaymentResponse> ProcessPaymentAsync(PspPaymentRequest request, CancellationToken cancellationToken = default);
}