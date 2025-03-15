namespace PandaMQ.Abstractions;

public record ServerGreetingMessage(Guid Id, Version Version, string? InformationalVersion) : IPandaMQMessage
{
    public static ServerGreetingMessage Default { get; } =
        new ServerGreetingMessage(Guid.Empty, new Version(1, 0), "PandaMQ Pre-Alpha");

    public static ServerGreetingMessage Create() => Default with { Id = Guid.NewGuid() };
}