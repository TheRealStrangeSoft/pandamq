using System.IO.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using PandaMQ.Server.Abstractions;

namespace PandaMQ.Server.Core;

public class WebSocketPipeFactory : IWebSocketPipeFactory
{
    private readonly IServiceProvider _serviceProvider;

    public WebSocketPipeFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public IWebSocketPipe<T> Create<T>(PipeOptions pipeOptions) where T : class
    {
        return ActivatorUtilities.CreateInstance<WebSocketPipe<T>>(_serviceProvider, pipeOptions);
    }
}