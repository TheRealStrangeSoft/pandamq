using Microsoft.AspNetCore.WebSockets;
using PandaMQ.Server.Abstractions;
using PandaMQ.Server.Core;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.AddServerServices();
builder.Services.AddWebSockets(options =>
{
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.KeepAliveTimeout = TimeSpan.FromSeconds(30);
    options.AllowedOrigins.Add("*");
});

var app = builder.Build();
app.UseWebSockets();

var websocketApi = app.MapGroup("/ws");
websocketApi.Map("/",
    (HttpContext context, IWebSocketHandler websocketHandler) =>
        websocketHandler.HandleAsync(context, context.RequestAborted));
websocketApi.Map("/json",
    (HttpContext context, IJsonWebSocketHandler websocketHandler) =>
        websocketHandler.HandleAsync(context, context.RequestAborted));

await app.RunAsync().ConfigureAwait(false);