namespace PandaMQ.Abstractions;

public record UnsubscribeMessage(string TopicPattern, string GroupId) : IPandaMQMessage;