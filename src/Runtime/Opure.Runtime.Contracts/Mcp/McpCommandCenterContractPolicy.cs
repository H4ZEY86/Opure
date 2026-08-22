namespace Opure.Runtime.Contracts.Mcp;

public static class McpCommandCenterContractPolicy
{
    public const string ServiceMethod = "mcp_command_center.get_tools";
    public const int MaximumRequestBytes = 4096;
    public const int MaximumResponseBytes = 1048576; // 1 MB
}
