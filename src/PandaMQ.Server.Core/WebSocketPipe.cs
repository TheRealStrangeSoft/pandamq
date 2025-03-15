using System.Buffers;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using PandaMQ.Abstractions;
using PandaMQ.Server.Abstractions;

namespace PandaMQ.Server.Core;

public sealed class WebSocketPipe<T> : IWebSocketPipe<T> where T : class
{
    private readonly IJsonSerializer<T> _serializer;
    private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
    private readonly Pipe _outboundPipe;
    private readonly Pipe _inboundPipe;
    private bool _disposed;

    public WebSocketPipe(
        IJsonSerializer<T> serializer,
        PipeOptions pipeOptions)
    {
        _serializer = serializer;
        _inboundPipe = new Pipe(pipeOptions);
        _outboundPipe = new Pipe(pipeOptions);
    }

    private void ThrowIfDisposed()
    {
        if (!_disposed) return;
        throw new ObjectDisposedException(nameof(WebSocketPipe<T>));
    }

    private static void ThrowIfInvalidMessageType(WebSocketMessageType messageType)
    {
        if (messageType != WebSocketMessageType.Text && messageType != WebSocketMessageType.Close)
        {
            throw new InvalidWebSocketMessageTypeException(messageType);
        }
    }

    private static bool ShouldStop(WebSocketMessageType messageType, CancellationToken cancellationToken)
    {
        return messageType == WebSocketMessageType.Close || cancellationToken.IsCancellationRequested;
    }

    private async Task ProcessNextAsync(WebSocket webSocket, PipeWriter pipeWriter, CancellationToken cancellationToken)
    {
        var receiveResult = await webSocket.ReceiveAsync(pipeWriter.GetMemory(), cancellationToken)
            .ConfigureAwait(false);
        if (ShouldStop(receiveResult.MessageType, cancellationToken))
        {
            return;
        }

        ThrowIfInvalidMessageType(receiveResult.MessageType);

        pipeWriter.Advance(receiveResult.Count);
        if (!receiveResult.EndOfMessage) return;
        await pipeWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task FillPipeAsync(WebSocket webSocket, PipeWriter pipeWriter, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await ProcessNextAsync(webSocket, pipeWriter, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (FatalWebSocketException)
        {
            throw;
        }
        catch (Exception ex)
        {
            FatalWebSocketException.UnhandledException(ex);
        }
        finally
        {
            await pipeWriter.CompleteAsync();
        }
    }

    private async IAsyncEnumerable<T> DrainPipeAsync(PipeReader pipeReader,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using var pipeStream = pipeReader.AsStream();
        await foreach (var message in _serializer.DeserializeStreamAsync(pipeStream, cancellationToken))
        {
            yield return message;
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }
        }
    }

    private async Task DrainPipeAsync(PipeReader pipeReader, Func<T, CancellationToken, Task> callback,
        CancellationToken cancellationToken)
    {
        await foreach (var message in DrainPipeAsync(pipeReader, cancellationToken))
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            await callback.Invoke(message, cancellationToken).ConfigureAwait(false);
        }
    }

    private readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

    public async Task SendAsync(T message, CancellationToken cancellationToken)
    {
        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await using var stream = _outboundPipe.Writer.AsStream(leaveOpen: true);
            await _serializer.SerializeAsync(message, stream, cancellationToken);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private async Task DrainOutboundPipeAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var result = await _outboundPipe.Reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            if (result.IsCompleted || result.IsCanceled)
            {
                return;
            }

            var buffer = result.Buffer;
            var endIndex = result.Buffer.PositionOf((byte)0);
            if (endIndex is not null)
            {
                var chunk = buffer.Slice(0, endIndex.Value);
                buffer = buffer.Slice(buffer.GetPosition(1, endIndex.Value));
                var bytes = 0;
                foreach (var segment in chunk)
                {
                    if (segment.IsEmpty && chunk.Length == 0)
                    {
                        break;
                    }

                    bytes += segment.Length;
                    var endOfMessage = bytes >= chunk.Length;
                    await webSocket.SendAsync(segment, WebSocketMessageType.Text, endOfMessage, cancellationToken);
                }
            }

            _outboundPipe.Reader.AdvanceTo(buffer.Start, buffer.End);
        }
    }

    public async Task RunAsync(WebSocket webSocket, Func<T, CancellationToken, Task> onNextMessageAsync,
        CancellationToken cancellationToken)
    {
        ThrowIfDisposed();
        using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task? fillPipeTask = null;
        Task? drainPipeTask = null;
        Task? drainOutboundPipeTask = null;
        try
        {
            fillPipeTask = FillPipeAsync(webSocket, _inboundPipe.Writer, cancellationTokenSource.Token);
            drainPipeTask = DrainPipeAsync(_inboundPipe.Reader, onNextMessageAsync, cancellationTokenSource.Token);
            drainOutboundPipeTask = DrainOutboundPipeAsync(webSocket, cancellationTokenSource.Token);
            var firstCompletedTask = await Task.WhenAny(drainPipeTask, fillPipeTask, drainOutboundPipeTask)
                .ConfigureAwait(false);
            await firstCompletedTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (FatalWebSocketException ex)
        {
            _inboundPipe.Writer.CompleteAsync(ex).Orphan();
            _outboundPipe.Writer.CompleteAsync(ex).Orphan();
            throw;
        }
        catch (Exception ex)
        {
            _inboundPipe.Writer.CompleteAsync(ex).Orphan();
            _outboundPipe.Writer.CompleteAsync(ex).Orphan();
            throw new FatalWebSocketException("An unhandled exception occurred",
                WebSocketCloseStatus.InternalServerError, ex);
        }
        finally
        {
            fillPipeTask?.Orphan();
            drainPipeTask?.Orphan();
            drainOutboundPipeTask?.Orphan();
            _inboundPipe.Writer.CompleteAsync().Orphan();
            _inboundPipe.Reader.CancelPendingRead();
            _outboundPipe.Writer.CompleteAsync().Orphan();
            _outboundPipe.Reader.CancelPendingRead();
            await _cancellationTokenSource.CancelAsync();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            _outboundPipe.Reader.Complete();
        }
        catch
        {
            // Ignored;
        }

        try
        {
            _outboundPipe.Writer.Complete();
        }
        catch
        {
            // Ignored.
        }

        _cancellationTokenSource.Dispose();
        _sendLock.Dispose();
    }
}