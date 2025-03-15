namespace PandaMQ.Abstractions;

public interface IJsonSerializer<T> where T : class
{
    public T Deserialize(ReadOnlySpan<byte> data);
    public byte[] Serialize(T obj);
    public void Serialize(T obj, Stream stream);
}