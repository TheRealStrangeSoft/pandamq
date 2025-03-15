using PandaMQ.Abstractions;

namespace PandaMQ.Server.Abstractions;

public interface IMessageClient
{
    Guid Id { get; }
    ValueTask SendMessageAsync(ServerEnvelope message, CancellationToken cancellationToken);
    ValueTask DisconnectAsync(CancellationToken cancellationToken);
}