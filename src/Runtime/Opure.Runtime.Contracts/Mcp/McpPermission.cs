using System.Collections.Generic;
using Opure.Runtime.Contracts.Providers;

namespace Opure.Runtime.Contracts.Mcp;

public sealed record McpPermission(
    string PermissionId,
    string ServerId,
    IReadOnlyList<string> AllowedTools,
    ApprovalStatus Status = ApprovalStatus.Pending);
