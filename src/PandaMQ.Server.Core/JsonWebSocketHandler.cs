using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;
using PandaMQ.Abstractions;
using PandaMQ.Server.Abstractions;

namespace PandaMQ.Server.Core;

public sealed class JsonWebSocketHandler : IJsonWebSocketHandler, IMessageClient, IDisposable
{
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private readonly IWebSocketPipe<IEnvelope> _pipe;
    private readonly IMessageServer _messageServer;

    public JsonWebSocketHandler(IWebSocketPipeFactory pipeFactory, IMessageServer messageServer)
    {
        _pipe = pipeFactory.Create<IEnvelope>();
        _messageServer = messageServer;
    }

    public void Dispose()
    {
        _pipe.Dispose();
        _cancellationTokenSource.Dispose();
    }

    public async Task HandleAsync(HttpContext context, CancellationToken cancellationToken)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("Not a WebSocket request", cancellationToken).ConfigureAwait(false);
            return;
        }

        using var webSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
        try
        {
            using var linkedCancellationTokenSource =
                CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token, cancellationToken);
            _messageServer.RegisterClientAsync(this, cancellationToken).Orphan();
            await _pipe.RunAsync(webSocket, OnNextMessageAsync, linkedCancellationTokenSource.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested ||
                                                 _cancellationTokenSource.IsCancellationRequested)
        {
            TryCloseWebSocketAsync(webSocket, cancellationToken: cancellationToken).Orphan();
        }
        catch (Exception ex)
        {
            var closeStatus = WebSocketCloseStatus.InternalServerError;
            var message = "An unhandled exception occurred";
            if (ex is FatalWebSocketException fatalWebSocketException)
            {
                message = fatalWebSocketException.Message;
                closeStatus = fatalWebSocketException.CloseStatus;
            }

            TryCloseWebSocketAsync(
                webSocket,
                closeStatus,
                message,
                cancellationToken).Orphan();
        }
        finally
        {
            await _cancellationTokenSource.CancelAsync();
            _messageServer.UnregisterClient(this);
            TryCloseWebSocketAsync(webSocket, cancellationToken: cancellationToken).Orphan();
        }
    }

    private async Task OnNextMessageAsync(IEnvelope envelope, CancellationToken cancellationToken)
    {
        if (envelope is not ClientEnvelope clientEnvelope)
        {
            throw new InvalidOperationException("Invalid message, expected client envelope");
        }

        await _messageServer.HandleMessageAsync(this, clientEnvelope, cancellationToken);
    }

    public Guid Id { get; } = Guid.NewGuid();

    public async ValueTask SendMessageAsync(ServerEnvelope message,
        CancellationToken cancellationToken)
    {
        await _pipe.SendAsync(message, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisconnectAsync(CancellationToken cancellationToken)
    {
        if (_cancellationTokenSource.IsCancellationRequested) return;
        await _cancellationTokenSource.CancelAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task TryCloseWebSocketAsync(WebSocket webSocket,
        WebSocketCloseStatus closeStatus = WebSocketCloseStatus.NormalClosure, string? message = null,
        CancellationToken cancellationToken = default)
    {
        if (webSocket.State != WebSocketState.Open) return;

        try
        {
            await webSocket.CloseAsync(closeStatus, message ?? string.Empty, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            try
            {
                webSocket.Abort();
            }
            catch
            {
                // Ignored.
            }
        }
        finally
        {
            await DisconnectAsync(CancellationToken.None);
        }
    }
}