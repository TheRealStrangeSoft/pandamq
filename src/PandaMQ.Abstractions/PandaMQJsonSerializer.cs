using System.Text.Json;

namespace PandaMQ.Abstractions;

public sealed class PandaMQJsonSerializer : IJsonSerializer<IEnvelope>
{
    private static readonly JsonSerializerOptions SerializerOptions = PandaMQJsonSerializerContext.Default.Options;

    public IEnvelope Deserialize(ReadOnlySpan<byte> data)
    {
        return JsonSerializer.Deserialize<IEnvelope>(data, SerializerOptions) ??
               throw new JsonException("Deserialized null message");
    }

    public void Serialize(IEnvelope message, Stream stream)
    {
        JsonSerializer.Serialize(stream, message, SerializerOptions);
    }

    public byte[] Serialize(IEnvelope message)
    {
        return JsonSerializer.SerializeToUtf8Bytes(message, SerializerOptions);
    }
}