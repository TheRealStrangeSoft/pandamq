namespace PandaMQ.Abstractions;

public record ClientEnvelope(IPandaMQMessage Message) : IEnvelope;