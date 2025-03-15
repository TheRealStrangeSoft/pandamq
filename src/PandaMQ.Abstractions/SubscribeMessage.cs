namespace PandaMQ.Abstractions;

public record SubscribeMessage(string TopicPattern, string GroupId) : IPandaMQMessage;