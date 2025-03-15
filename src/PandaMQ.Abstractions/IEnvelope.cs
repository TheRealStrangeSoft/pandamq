using System.Text.Json.Serialization;

namespace PandaMQ.Abstractions;

[JsonDerivedType(typeof(ServerEnvelope), "server")]
[JsonDerivedType(typeof(ClientEnvelope), "client")]
public interface IEnvelope
{
    IPandaMQMessage Message { get; }
}