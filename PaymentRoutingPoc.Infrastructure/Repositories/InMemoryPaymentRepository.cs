namespace PaymentRoutingPoc.Infrastructure.Repositories;

using Domain.Aggregates;
using Domain.Repositories;

public class InMemoryPaymentRepository : InMemoryRepository<Payment>,IPaymentRepository;