namespace PaymentRoutingPoc.Domain.ValueObjects;

using ValueOf;

public class PaymentStatus : ValueOf<int, PaymentStatus>
{
    private const int PendingValue = 0;
    private const int ProcessedValue = 1;
    private const int FailedValue = 2;

    private static readonly IReadOnlyDictionary<int, IReadOnlySet<int>> AllowedTransitions =
        new Dictionary<int, IReadOnlySet<int>>
        {
            [PendingValue] = new HashSet<int> { ProcessedValue, FailedValue },
            [ProcessedValue] = new HashSet<int>(),
            [FailedValue] = new HashSet<int>()
        };

    public static PaymentStatus Pending => From(PendingValue);
    public static PaymentStatus Processed => From(ProcessedValue);
    public static PaymentStatus Failed => From(FailedValue);

    public string Name => Value switch
    {
        PendingValue => nameof(Pending),
        ProcessedValue => nameof(Processed),
        FailedValue => nameof(Failed),
        _ => "Unknown"
    };

    public bool CanTransitionTo(PaymentStatus targetStatus)
    {
        if (targetStatus == null)
            throw new ArgumentNullException(nameof(targetStatus));

        return AllowedTransitions[Value].Contains(targetStatus.Value);
    }

    public PaymentStatus TransitionTo(PaymentStatus targetStatus)
    {
        if (!CanTransitionTo(targetStatus))
            throw new InvalidOperationException($"Invalid payment status transition from {Name} to {targetStatus.Name}");

        return targetStatus;
    }

    protected override void Validate()
    {
        if (Value is not (PendingValue or ProcessedValue or FailedValue))
            throw new ArgumentOutOfRangeException(nameof(Value), $"Unsupported payment status value: {Value}");
    }

    public override string ToString()
    {
        return Name;
    }
}
