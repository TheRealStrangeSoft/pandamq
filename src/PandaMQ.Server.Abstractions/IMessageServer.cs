using PandaMQ.Abstractions;

namespace PandaMQ.Server.Abstractions;

public interface IMessageServer
{
    public Task RegisterClientAsync(IMessageClient client, CancellationToken cancellationToken);
    public void UnregisterClient(IMessageClient client);
    public Task HandleMessageAsync(IMessageClient client, ClientEnvelope envelope, CancellationToken cancellationToken);
    Task NotifyMessageSentAsync(IMessageClient client, ServerEnvelope message);
}