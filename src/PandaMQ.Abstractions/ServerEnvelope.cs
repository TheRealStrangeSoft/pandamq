using System.Text.Json.Serialization;

namespace PandaMQ.Abstractions;

public record ServerEnvelope(IPandaMQMessage Message, DateTimeOffset Timestamp) : IEnvelope
{
    [JsonIgnore]
    public Guid Id { get; init; } = Guid.NewGuid();
    public static ServerEnvelope ForMessage<T>(T message) where T : class, IPandaMQMessage =>
        new ServerEnvelope(message, DateTimeOffset.UtcNow);
}