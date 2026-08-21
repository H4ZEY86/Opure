namespace Opure.Runtime.Contracts.Mcp;

public sealed record McpServerProfile(
    string ServerId,
    string Name,
    string ExecutablePath,
    string Sha256Fingerprint);
