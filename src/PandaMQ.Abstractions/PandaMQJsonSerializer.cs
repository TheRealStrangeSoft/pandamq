using System.Runtime.CompilerServices;
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

    public async Task SerializeAsync(IEnvelope message, Stream stream, CancellationToken cancellationToken)
    {
        await JsonSerializer.SerializeAsync(stream, message, SerializerOptions, cancellationToken);
    }

    public async IAsyncEnumerable<IEnvelope> DeserializeStreamAsync(Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var message in
                       JsonSerializer.DeserializeAsyncEnumerable<IEnvelope>(stream, SerializerOptions,
                           cancellationToken))
        {
            if (message is null)
            {
                throw new JsonException("Deserialized null message");
            }

            yield return message;
        }
    }

    public byte[] Serialize(IEnvelope message)
    {
        return JsonSerializer.SerializeToUtf8Bytes(message, SerializerOptions);
    }
}