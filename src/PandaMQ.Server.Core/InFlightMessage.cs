using PandaMQ.Abstractions;

namespace PandaMQ.Server.Core;

public sealed record InFlightMessage(Guid ClientId, ServerEnvelope Message);