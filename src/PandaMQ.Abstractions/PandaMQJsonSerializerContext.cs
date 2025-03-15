using System.Text.Json.Serialization;

namespace PandaMQ.Abstractions;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    GenerationMode = JsonSourceGenerationMode.Serialization | JsonSourceGenerationMode.Metadata)]
[JsonSerializable(typeof(IPandaMQMessage))]
[JsonSerializable(typeof(ClientEnvelope))]
[JsonSerializable(typeof(ServerEnvelope))]
[JsonSerializable(typeof(IEnvelope))]
public partial class PandaMQJsonSerializerContext : JsonSerializerContext;