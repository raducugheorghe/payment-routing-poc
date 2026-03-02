namespace PaymentRoutingPoc.Application.DTOs;

using PaymentRoutingPoc.Domain.ValueObjects;

public class PaymentResponse
{
    public Guid PaymentId { get; set; }
    public PaymentStatus Status { get; set; }
    public string Message { get; set; } = string.Empty;
}

