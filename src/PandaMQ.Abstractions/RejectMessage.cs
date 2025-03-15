namespace PandaMQ.Abstractions;

public record RejectMessage(Guid Id, string Topic) : IPandaMQMessage;