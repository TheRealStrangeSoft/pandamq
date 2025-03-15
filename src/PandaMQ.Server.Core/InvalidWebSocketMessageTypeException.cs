using System.Net.WebSockets;

namespace PandaMQ.Server.Core;

internal class InvalidWebSocketMessageTypeException(WebSocketMessageType messageType)
    : FatalWebSocketException($"Unsupported message type: {messageType}", WebSocketCloseStatus.InvalidMessageType);