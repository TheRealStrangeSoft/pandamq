namespace PandaMQ.Abstractions;

public record ClientHelloMessage(string Name) : IPandaMQMessage;