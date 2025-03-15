using System.Collections.Immutable;
using System.Text.Json;

namespace PandaMQ.Abstractions;

public record DeliverMessage(
    Guid Id,
    string Topic,
    ImmutableDictionary<string, ImmutableList<string>> Headers,
    JsonElement Payload,
    DateTimeOffset Timestamp) : IPandaMQMessage;