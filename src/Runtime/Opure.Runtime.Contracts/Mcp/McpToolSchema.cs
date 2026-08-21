namespace Opure.Runtime.Contracts.Mcp;

public sealed record McpToolSchema(
    string ToolName,
    string Description,
    string InputSchemaJson);
