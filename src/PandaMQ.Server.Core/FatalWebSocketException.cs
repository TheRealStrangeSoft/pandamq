using System.Net.WebSockets;

namespace PandaMQ.Server.Core;

internal class FatalWebSocketException : InvalidOperationException
{
    public FatalWebSocketException(string message,
        WebSocketCloseStatus closeStatus = WebSocketCloseStatus.InternalServerError,
        Exception? innerException = null) : base(message, innerException)
    {
        CloseStatus = closeStatus;
    }

    public static FatalWebSocketException UnhandledException(Exception ex) =>
        new FatalWebSocketException("An unhandled exception occurred", WebSocketCloseStatus.InternalServerError, ex);

    public WebSocketCloseStatus CloseStatus { get; }
}