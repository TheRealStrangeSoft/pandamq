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