using System.Text.Json.Serialization;
using Opure.Runtime.Contracts.Mcp;

namespace Opure.Runtime.Mcp;

[JsonSerializable(typeof(JsonRpcRequest))]
[JsonSerializable(typeof(JsonRpcResponse))]
[JsonSerializable(typeof(JsonRpcError))]
[JsonSerializable(typeof(McpInitializeParams))]
[JsonSerializable(typeof(McpClientInfo))]
[JsonSerializable(typeof(McpCallToolParams))]
[JsonSerializable(typeof(McpCallToolResult))]
[JsonSerializable(typeof(McpListToolsResult))]
[JsonSerializable(typeof(McpToolSchemaDto))]
[JsonSerializable(typeof(McpToolSchema))]
public partial class McpJsonSerializerContext : JsonSerializerContext
{
}
