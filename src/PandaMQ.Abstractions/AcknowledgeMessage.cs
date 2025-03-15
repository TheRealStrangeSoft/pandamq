namespace PandaMQ.Abstractions;

public record AcknowledgeMessage(
    Guid Id,
    string Topic)
    : IPandaMQMessage;