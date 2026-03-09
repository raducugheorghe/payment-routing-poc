using System.Text.Json;
using PaymentRoutingPoc.Domain.Events;

namespace PaymentRoutingPoc.Persistence.Serialization;

/// <summary>
/// Handles serialization and deserialization of domain events.
/// Supports event versioning for schema evolution.
/// </summary>
public class EventSerializer
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Type mapping from event type names to CLR types.
    /// Supports versioning by allowing v1, v2, etc. event types.
    /// </summary>
    private static readonly Dictionary<string, Type> TypeMap = new()
    {
        // Current versions
        ["PaymentSubmittedEvent"] = typeof(PaymentSubmittedEvent),
        ["PaymentSucceededEvent"] = typeof(PaymentSucceededEvent),
        ["PaymentFailedEvent"] = typeof(PaymentFailedEvent),

        // Future: add versioned types here
        // ["PaymentSubmittedEvent_v2"] = typeof(PaymentSubmittedEventV2),
    };

    /// <summary>
    /// Serializes a domain event to JSON.
    /// </summary>
    /// <param name="domainEvent">The event to serialize</param>
    /// <returns>JSON string representation of the event</returns>
    public string Serialize(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        try
        {
            return JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonOptions)
                   ?? throw new SerializationException($"Failed to serialize event of type {domainEvent.GetType().Name}");
        }
        catch (Exception ex)
        {
            throw new SerializationException(
                $"Error serializing event of type {domainEvent.GetType().Name}: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Serializes event metadata to JSON.
    /// </summary>
    /// <param name="metadata">The metadata to serialize</param>
    /// <returns>JSON string representation</returns>
    public string SerializeMetadata(EventMetadata? metadata)
    {
        if (metadata == null)
            return "{}";

        try
        {
            return JsonSerializer.Serialize(metadata, JsonOptions) 
                   ?? "{}";
        }
        catch (Exception ex)
        {
            throw new SerializationException($"Error serializing event metadata: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Deserializes a JSON string back to a domain event.
    /// Uses the event type name to locate the correct CLR type.
    /// </summary>
    /// <param name="json">JSON string containing the event data</param>
    /// <param name="eventType">The type name of the event (e.g., "PaymentSubmittedEvent")</param>
    /// <returns>Deserialized domain event</returns>
    /// <exception cref="SerializationException">If event type is unknown or deserialization fails</exception>
    public IDomainEvent Deserialize(string json, string eventType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        if (!TypeMap.TryGetValue(eventType, out var clrType))
        {
            throw new SerializationException(
                $"Unknown event type: {eventType}. Supported types: {string.Join(", ", TypeMap.Keys)}");
        }

        try
        {
            var result = JsonSerializer.Deserialize(json, clrType, JsonOptions);

            if (result is not IDomainEvent domainEvent)
            {
                throw new SerializationException(
                    $"Deserialized object is not an IDomainEvent. Type: {result?.GetType().Name ?? "null"}");
            }

            return domainEvent;
        }
        catch (JsonException ex)
        {
            throw new SerializationException(
                $"Error deserializing event of type {eventType}: {ex.Message}",
                ex);
        }
        catch (Exception ex) when (ex is not SerializationException)
        {
            throw new SerializationException(
                $"Unexpected error deserializing event of type {eventType}: {ex.Message}",
                ex);
        }
    }

    /// <summary>
    /// Deserializes event metadata from JSON.
    /// </summary>
    /// <param name="json">JSON string containing metadata</param>
    /// <returns>Deserialized metadata object</returns>
    public EventMetadata DeserializeMetadata(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new EventMetadata();

        try
        {
            return JsonSerializer.Deserialize<EventMetadata>(json, JsonOptions)
                   ?? new EventMetadata();
        }
        catch (Exception ex)
        {
            throw new SerializationException($"Error deserializing event metadata: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets all supported event type names.
    /// Useful for validation and debugging.
    /// </summary>
    public IEnumerable<string> GetSupportedEventTypes() => TypeMap.Keys;

    /// <summary>
    /// Registers a custom event type for serialization.
    /// Useful for plugin systems or event versioning.
    /// </summary>
    /// <param name="eventTypeName">The type name (e.g., "PaymentSubmittedEvent_v2")</param>
    /// <param name="clrType">The CLR type that implements IDomainEvent</param>
    public static void RegisterEventType(string eventTypeName, Type clrType)
    {
        if (string.IsNullOrWhiteSpace(eventTypeName))
            throw new ArgumentException("Event type name cannot be null or empty", nameof(eventTypeName));

        if (!typeof(IDomainEvent).IsAssignableFrom(clrType))
            throw new ArgumentException($"Type {clrType.Name} must implement IDomainEvent", nameof(clrType));

        TypeMap[eventTypeName] = clrType;
    }
}

/// <summary>
/// Exception thrown when event serialization or deserialization fails.
/// </summary>
public class SerializationException : Exception
{
    public SerializationException(string message) : base(message) { }
    public SerializationException(string message, Exception innerException)
        : base(message, innerException) { }
}
