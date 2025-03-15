using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PandaMQ.Abstractions;
using PandaMQ.Server.Abstractions;

namespace PandaMQ.Server.Core;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServerServices(this IServiceCollection services)
    {
        services.TryAddScoped<IJsonWebSocketHandler, JsonWebSocketHandler>();
        services.TryAddScoped<IWebSocketHandler, JsonWebSocketHandler>();
        services.TryAddSingleton<IJsonSerializer<IEnvelope>, PandaMQJsonSerializer>();
        services.TryAddSingleton<IWebSocketPipeFactory, WebSocketPipeFactory>();
        services.TryAddSingleton<IMessageServer, MessageServer>();
        return services;
    }
}