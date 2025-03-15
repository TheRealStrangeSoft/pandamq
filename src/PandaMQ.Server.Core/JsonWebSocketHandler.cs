using System.Net.WebSockets;
using System.Threading.Channels;
using Microsoft.AspNetCore.Http;
using PandaMQ.Abstractions;
using PandaMQ.Server.Abstractions;

namespace PandaMQ.Server.Core;

public sealed record InFlightMessage(Guid ClientId, ServerEnvelope Message);
public sealed class JsonWebSocketHandler : IJsonWebSocketHandler, IMessageClient, IDisposable
{
    private static readonly BoundedChannelOptions BoundedChannelOptions = new BoundedChannelOptions(1)
    {
        SingleReader = true,
        SingleWriter = false,
        FullMode = BoundedChannelFullMode.Wait,
        AllowSynchronousContinuations = false
    };

    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

    private readonly MemoryStream _memoryStream = new MemoryStream();

    private readonly Channel<ServerEnvelope> _messageChannel =
        Channel.CreateBounded<ServerEnvelope>(BoundedChannelOptions);

    private readonly IJsonSerializer<IEnvelope> _serializer;
    private readonly IMessageServer _messageServer;

    public JsonWebSocketHandler(IJsonSerializer<IEnvelope> serializer, IMessageServer messageServer)
    {
        _serializer = serializer;
        _messageServer = messageServer;
    }

    public void Dispose()
    {
        _memoryStream.Dispose();
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
            await _messageServer.RegisterClientAsync(this, cancellationToken).ConfigureAwait(false);
            await RunWebSocketAsync(cancellationToken, webSocket).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested ||
                                                 _cancellationTokenSource.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            await ShutdownAsync(webSocket, ex, cancellationToken).ConfigureAwait(false);
            await TryCloseWebSocketAsync(webSocket, WebSocketCloseStatus.InternalServerError,
                "An unhandled exception occurred", cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _messageServer.UnregisterClient(this);
            await ShutdownAsync(webSocket, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    public Guid Id { get; }

    public async ValueTask SendMessageAsync(ServerEnvelope message,
        CancellationToken cancellationToken)
    {
        await _messageChannel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask DisconnectAsync(CancellationToken cancellationToken)
    {
        if (_cancellationTokenSource.IsCancellationRequested) return;

        await _cancellationTokenSource.CancelAsync().WaitAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task RunWebSocketAsync(CancellationToken cancellationToken, WebSocket webSocket)
    {
        using var linkedCancellationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token);

        var readTask = ReadWebSocketAsync(webSocket, linkedCancellationTokenSource.Token);
        var writeTask = WriteWebSocketAsync(webSocket, linkedCancellationTokenSource.Token);
        await Task.WhenAll(readTask, writeTask).ConfigureAwait(false);
    }

    private async Task ShutdownAsync(WebSocket webSocket, Exception? exception = null,
        CancellationToken cancellationToken = default)
    {
        if(_messageChannel.Reader.Completion.IsCompleted) return;
        var closeStatus = WebSocketCloseStatus.NormalClosure;
        var message = string.Empty;

        if (
            (exception != null && exception is not OperationCanceledException) ||
            (_cancellationTokenSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        )
        {
            closeStatus = WebSocketCloseStatus.InternalServerError;
            message = "Internal server error";
        }

        await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
        _messageChannel.Writer.TryComplete(exception);
        await TryCloseWebSocketAsync(webSocket, closeStatus, message, CancellationToken.None).ConfigureAwait(false);
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
            await _cancellationTokenSource.CancelAsync().ConfigureAwait(false);
        }
    }

    private async Task WriteWebSocketAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var message in _messageChannel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                await SendWebSocketMessageAsync(webSocket, message, cancellationToken).ConfigureAwait(false);
            // TODO
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task SendWebSocketMessageAsync(WebSocket webSocket, ServerEnvelope message,
        CancellationToken cancellationToken)
    {
        _memoryStream.Seek(0, SeekOrigin.Begin);
        _memoryStream.SetLength(0);
        _serializer.Serialize(message, _memoryStream);
        await _memoryStream.FlushAsync(cancellationToken).ConfigureAwait(false);
        _memoryStream.Seek(0, SeekOrigin.Begin);
        await webSocket.SendAsync(_memoryStream.ToArray(), WebSocketMessageType.Text, true, cancellationToken).ConfigureAwait(false);
        await _messageServer.NotifyMessageSentAsync(this, message);
    }

    private Task ReadWebSocketAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}