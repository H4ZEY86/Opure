using System.Text.Json.Serialization;

namespace Opure.Patch.Contracts;

[JsonSerializable(typeof(ExecutePatchCommand))]
[JsonSerializable(typeof(PatchExecutionResult))]
[JsonSerializable(typeof(UnifiedPatchProposal))]
[JsonSerializable(typeof(UnifiedHunk))]
[JsonSerializable(typeof(UnifiedHunkLine))]
public partial class PatchExecutionJsonContext : JsonSerializerContext
{
}
