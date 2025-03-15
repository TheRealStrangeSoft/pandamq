namespace PandaMQ.Abstractions;

public interface IJsonSerializer<T> where T : class
{
    public T Deserialize(ReadOnlySpan<byte> data);
    public byte[] Serialize(T obj);
    public Task SerializeAsync(T obj, Stream stream, CancellationToken cancellationToken);
    public IAsyncEnumerable<T> DeserializeStreamAsync(Stream stream, CancellationToken cancellationToken = default);
}