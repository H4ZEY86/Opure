using System.Text.Json.Serialization;

namespace Opure.Runtime.Contracts.Models;

/// <summary>
/// Source generator context for StreamPayload serialization/deserialization.
/// </summary>
[JsonSerializable(typeof(StreamPayload))]
[JsonSerializable(typeof(ToolRequest))]
[JsonSerializable(typeof(ModelRequest))]
public partial class ModelContractsJsonContext : JsonSerializerContext
{
}
