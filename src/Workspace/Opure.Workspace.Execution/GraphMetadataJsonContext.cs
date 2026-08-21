using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Opure.Workspace.Execution;

[JsonSerializable(typeof(Dictionary<string, string>))]
[JsonSerializable(typeof(IReadOnlyDictionary<string, string>))]
public sealed partial class GraphMetadataJsonContext : JsonSerializerContext
{
}
