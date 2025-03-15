using System.Net.WebSockets;

namespace PandaMQ.Server.Abstractions;

public interface IWebSocketPipe<T> : IDisposable where T : class
{
    public Task SendAsync(T message, CancellationToken cancellationToken);

    public Task RunAsync(WebSocket webSocket, Func<T, CancellationToken, Task> onNextMessageAsync,
        CancellationToken cancellationToken);
}