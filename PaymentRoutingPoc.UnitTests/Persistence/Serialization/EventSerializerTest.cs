using PaymentRoutingPoc.Domain.Events;
using PaymentRoutingPoc.Persistence.Serialization;

namespace PaymentRoutingPoc.UnitTests.Persistence.Serialization;

public class EventSerializerTest
{
    private readonly EventSerializer _serializer = new();

    [Fact]
    public void SerializeDeserialize_SubmittedEvent_ShouldRoundTrip()
    {
        var occurredAt = DateTime.UtcNow.AddMinutes(-5);
        var eventToSerialize = new PaymentSubmittedEvent(
            Guid.NewGuid(),
            123.45m,
            "USD",
            Guid.NewGuid(),
            "1111",
            Guid.NewGuid(),
            "Merchant A",
            7,
            occurredAt);

        var json = _serializer.Serialize(eventToSerialize);
        var deserialized = _serializer.Deserialize(json, nameof(PaymentSubmittedEvent));

        var submitted = Assert.IsType<PaymentSubmittedEvent>(deserialized);
        Assert.Equal(eventToSerialize.PaymentId, submitted.PaymentId);
        Assert.Equal(eventToSerialize.Amount, submitted.Amount);
        Assert.Equal(eventToSerialize.Currency, submitted.Currency);
        Assert.Equal(eventToSerialize.CardId, submitted.CardId);
        Assert.Equal(eventToSerialize.CardLast4, submitted.CardLast4);
        Assert.Equal(eventToSerialize.MerchantId, submitted.MerchantId);
        Assert.Equal(eventToSerialize.MerchantName, submitted.MerchantName);
        Assert.Equal(eventToSerialize.AggregateVersion, submitted.AggregateVersion);
        Assert.Equal(occurredAt, submitted.OccurredAt);
    }

    [Fact]
    public void Deserialize_UnknownType_ShouldThrowSerializationException()
    {
        var json = "{}";

        Assert.Throws<SerializationException>(() => _serializer.Deserialize(json, "UnknownEventType"));
    }

    [Fact]
    public void SerializeDeserialize_Metadata_ShouldRoundTrip()
    {
        var metadata = new EventMetadata
        {
            IdempotencyKey = "idem-1",
            CorrelationId = "corr-1",
            CausationId = "cause-1",
            RequestId = "req-1",
            UserId = "user-1"
        };

        var json = _serializer.SerializeMetadata(metadata);
        var roundTrip = _serializer.DeserializeMetadata(json);

        Assert.Equal(metadata.IdempotencyKey, roundTrip.IdempotencyKey);
        Assert.Equal(metadata.CorrelationId, roundTrip.CorrelationId);
        Assert.Equal(metadata.CausationId, roundTrip.CausationId);
        Assert.Equal(metadata.RequestId, roundTrip.RequestId);
        Assert.Equal(metadata.UserId, roundTrip.UserId);
    }
}
