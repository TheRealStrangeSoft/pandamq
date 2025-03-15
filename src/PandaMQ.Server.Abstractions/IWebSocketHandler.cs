using Microsoft.AspNetCore.Http;

namespace PandaMQ.Server.Abstractions;

public interface IWebSocketHandler
{
    Task HandleAsync(HttpContext context, CancellationToken cancellationToken);
}