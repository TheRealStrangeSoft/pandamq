using System.IO.Pipelines;

namespace PandaMQ.Server.Abstractions;

public interface IWebSocketPipeFactory
{
    public IWebSocketPipe<T> Create<T>() where T : class => Create<T>(PipeOptions.Default);
    IWebSocketPipe<T> Create<T>(PipeOptions @default) where T : class;
}