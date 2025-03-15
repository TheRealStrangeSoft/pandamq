using System.Text.Json;
using HeaderDictionary =
    System.Collections.Immutable.ImmutableDictionary<string, System.Collections.Immutable.ImmutableList<string>>;

namespace PandaMQ.Abstractions;

public record PublishMessage(
    Guid Id,
    string Topic,
    HeaderDictionary Headers,
    JsonElement Payload
) : IPandaMQMessage;