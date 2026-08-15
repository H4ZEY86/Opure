namespace Opure.Patch.Protocol;

/// <summary>
/// Transport limits and method constants for the Patch Review gRPC service.
/// </summary>
public static class PatchReviewContractPolicy
{
    // Typical request payloads are small IDs and SHA-256 hex strings.
    public const int MaximumRequestBytes = 16 * 1024;

    // Preview responses may include diff text; 256 KB provides headroom.
    public const int MaximumResponseBytes = 256 * 1024;

    public const string GetActivePatchesMethod =
        "/opure.patch.protocol.PatchReview/GetActivePatches";
    public const string GetPatchPreviewMethod =
        "/opure.patch.protocol.PatchReview/GetPatchPreview";
    public const string ApprovePatchMethod =
        "/opure.patch.protocol.PatchReview/ApprovePatch";
    public const string CancelPatchMethod =
        "/opure.patch.protocol.PatchReview/CancelPatch";
}
