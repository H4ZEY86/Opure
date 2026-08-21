using System.Text.Json;
using System.Text.Json.Serialization;

namespace Opure.Runtime.Mcp;

public sealed record JsonRpcRequest(
    [property: JsonPropertyName("jsonrpc")] string JsonRpc,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("method")] string Method,
    [property: JsonPropertyName("params")] JsonElement? Params);

public sealed record JsonRpcResponse(
    [property: JsonPropertyName("jsonrpc")] string JsonRpc,
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("result")] JsonElement? Result,
    [property: JsonPropertyName("error")] JsonRpcError? Error);

public sealed record JsonRpcError(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("data")] JsonElement? Data);

public sealed record McpInitializeParams(
    [property: JsonPropertyName("protocolVersion")] string ProtocolVersion,
    [property: JsonPropertyName("capabilities")] JsonElement Capabilities,
    [property: JsonPropertyName("clientInfo")] McpClientInfo ClientInfo);

public sealed record McpClientInfo(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("version")] string Version);

public sealed record McpCallToolParams(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("arguments")] JsonElement? Arguments);

public sealed record McpCallToolResult(
    [property: JsonPropertyName("content")] JsonElement Content,
    [property: JsonPropertyName("isError")] bool? IsError);

public sealed record McpListToolsResult(
    [property: JsonPropertyName("tools")] McpToolSchemaDto[] Tools);

public sealed record McpToolSchemaDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("inputSchema")] JsonElement InputSchema);
