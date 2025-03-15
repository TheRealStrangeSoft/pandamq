using System.Collections.Concurrent;
using PandaMQ.Abstractions;
using PandaMQ.Server.Abstractions;

namespace PandaMQ.Server.Core;

public static class ClientExtensions
{
    public static async ValueTask SendMessageAsync<T>(this IMessageClient client, T message,
        CancellationToken cancellationToken) where T : class, IPandaMQMessage
    {
        await client.SendMessageAsync(ServerEnvelope.ForMessage(message), cancellationToken).ConfigureAwait(false);
    }
}

public class MessageServer : IMessageServer
{
    private readonly ConcurrentDictionary<Guid, IMessageClient> _clients =
        new ConcurrentDictionary<Guid, IMessageClient>();

    private ConcurrentDictionary<string, Topic> _topics =
        new ConcurrentDictionary<string, Topic>(StringComparer.OrdinalIgnoreCase);

    public async Task RegisterClientAsync(IMessageClient client, CancellationToken cancellationToken)
    {
        if (_clients.TryAdd(client.Id, client))
        {
            await client.SendMessageAsync(ServerGreetingMessage.Create(), cancellationToken).ConfigureAwait(false);
        }
    }

    public void UnregisterClient(IMessageClient client)
    {
        _clients.TryRemove(client.Id, out _);
    }

    public async Task HandleMessageAsync(IMessageClient client, ClientEnvelope envelope,
        CancellationToken cancellationToken)
    {
        if (!_clients.ContainsKey(client.Id))
        {
            throw new InvalidOperationException("Client not registered");
        }

        switch (envelope.Message)
        {
            case SubscribeMessage subscribeMessage:
            // TODO
            case UnsubscribeMessage unsubscribeMessage:
            // TODO
            case PublishMessage publishMessage:
            // TODO
            case AcknowledgeMessage acknowledgeMessage:
            // TODO
            case RejectMessage rejectMessage:
            // TODO
            case ClientHelloMessage clientHelloMessage:
                // TODO
                break;
            default:
                await client.DisconnectAsync(cancellationToken).ConfigureAwait(false);
                break;
        }
    }

    public Task NotifyMessageSentAsync(IMessageClient client, ServerEnvelope message)
    {
        // TODO
        return Task.CompletedTask;
    }
}