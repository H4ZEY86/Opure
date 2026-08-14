using System;
using Opure.Patch.Contracts;

namespace Opure.Patch.Service;

/// <summary>
/// Binds and mathematically validates a developer's patch approval against the exact preview and proposal.
/// Fails closed on any cryptograhpic or identity mismatch.
/// </summary>
public sealed class PatchApprovalBinder
{
    public static void VerifyApproval(
        ExactUtf8PatchApproval approval,
        ExactUtf8PatchPreview preview,
        ExactUtf8PatchProposal proposal,
        string expectedApproverIdentity)
    {
        ArgumentNullException.ThrowIfNull(approval);
        ArgumentNullException.ThrowIfNull(preview);
        ArgumentNullException.ThrowIfNull(proposal);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedApproverIdentity);

        // 1. Structural Binding: Ensure all components belong to the same patch lifecycle
        if (!string.Equals(approval.PatchId, proposal.PatchId, StringComparison.Ordinal) ||
            !string.Equals(approval.PatchId, preview.PatchId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Approval rejected: Patch ID mismatch across lifecycle bounds.");
        }

        // 2. Cryptographic Proposal Binding: Ensure the approval explicitly binds the exact proposal hash
        if (!string.Equals(approval.ProposalSha256, proposal.ProposalSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Approval rejected: Cryptographic mismatch for Proposal SHA-256.");
        }

        // 3. Cryptographic Preview Binding: Ensure the approval explicitly binds the exact deterministic preview digest
        if (!string.Equals(approval.PreviewDigestSha256, preview.PreviewDigestSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Approval rejected: Cryptographic mismatch for Preview Digest SHA-256.");
        }

        // 4. Identity Binding: Ensure the approval was issued by the expected authorized identity
        if (!string.Equals(approval.ApproverIdentity, expectedApproverIdentity, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Approval rejected: Unauthorized approver identity.");
        }
    }
}
