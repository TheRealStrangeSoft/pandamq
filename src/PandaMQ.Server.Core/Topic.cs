using System.Collections.Concurrent;

namespace PandaMQ.Server.Core;

public class Topic
{
    private readonly ConcurrentDictionary<Guid, bool> _subscribers = new ConcurrentDictionary<Guid, bool>();

    public Topic(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public IEnumerable<Guid> GetSubscribers()
    {
        return _subscribers.Keys;
    }

    public void AddSubscriber(Guid subscriberId)
    {
        _subscribers.TryAdd(subscriberId, true);
    }

    public void RemoveSubscriber(Guid subscriberId)
    {
        _subscribers.TryRemove(subscriberId, out _);
    }
}